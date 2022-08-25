namespace Lib9c.Tests.Action.Scenario
{
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class CombinationAndRapidCombinationTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly TableSheets _tableSheets;
        private Address _agentAddress;
        private Address _avatarAddress;
        private Address _inventoryAddress;
        private Address _worldInformationAddress;
        private Address _questListAddress;
        private Address _slot0Address;

        public CombinationAndRapidCombinationTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            _slot0Address = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var slot0State = new CombinationSlotState(
                _slot0Address,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction),
            };

            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);

            _initialState = new Tests.Action.State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, avatarState.questList.Serialize())
                .SetState(_slot0Address, slot0State.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        // NOTE: Do not remove.
        // [Theory]
        // [InlineData(new[] { 1 })]
        // [InlineData(new[] { 1, 2 })]
        // [InlineData(new[] { 1, 3 })]
        // [InlineData(new[] { 1, 4 })]
        // [InlineData(new[] { 1, 2, 3 })]
        // [InlineData(new[] { 1, 2, 4 })]
        // [InlineData(new[] { 1, 3, 4 })]
        // [InlineData(new[] { 1, 2, 3, 4 })]
        // public void FindRandomSeedForCase(int[] optionNumbers)
        // {
        //     var randomSeed = 0;
        //     while (randomSeed < 100000)
        //     {
        //         try
        //         {
        //             Case(randomSeed, optionNumbers);
        //         }
        //         catch
        //         {
        //             randomSeed++;
        //             continue;
        //         }
        //
        //         Log.Debug(randomSeed.ToString());
        //         break;
        //     }
        // }
        [Theory]
        [InlineData(6, new[] { 1 })]
        [InlineData(0, new[] { 1, 2 })]
        [InlineData(7, new[] { 1, 3 })]
        [InlineData(9, new[] { 1, 4 })]
        [InlineData(2, new[] { 1, 2, 3 })]
        [InlineData(1, new[] { 1, 2, 4 })]
        [InlineData(5, new[] { 1, 3, 4 })]
        [InlineData(18, new[] { 1, 2, 3, 4 })]
        public void Case(int randomSeed, int[] optionNumbers)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            Assert.NotNull(gameConfigState);

            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList.First(e =>
                e.Options.Count == 4 &&
                e.RequiredBlockIndex > GameConfig.RequiredAppraiseBlock &&
                e.RequiredGold == 0);
            var recipeRow =
                _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(e => e.SubRecipeIds.Contains(subRecipeRow.Id));
            var combinationEquipmentAction = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeRow.Id,
                subRecipeId = subRecipeRow.Id,
            };

            var inventoryValue = _initialState.GetState(_inventoryAddress);
            Assert.NotNull(inventoryValue);

            var inventoryState = new Inventory((List)inventoryValue);
            inventoryState.AddFungibleItem(
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, recipeRow.MaterialId),
                recipeRow.MaterialCount);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                inventoryState.AddFungibleItem(
                    ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, materialInfo.Id),
                    materialInfo.Count);
            }

            var worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                recipeRow.UnlockStage);

            var unlockedRecipeIdsAddress = _avatarAddress.Derive("recipe_ids");
            var recipeIds = List.Empty;
            for (int i = 1; i < recipeRow.UnlockStage + 1; i++)
            {
                recipeIds = recipeIds.Add(i.Serialize());
            }

            var nextState = _initialState
                .SetState(unlockedRecipeIdsAddress, recipeIds)
                .SetState(_inventoryAddress, inventoryState.Serialize())
                .SetState(_worldInformationAddress, worldInformation.Serialize());

            var random = new TestRandom(randomSeed);
            nextState = combinationEquipmentAction.Execute(new ActionContext
            {
                PreviousStates = nextState,
                BlockIndex = 0,
                Random = random,
                Signer = _agentAddress,
            });

            var slot0Value = nextState.GetState(_slot0Address);
            Assert.NotNull(slot0Value);

            var slot0State = new CombinationSlotState((Dictionary)slot0Value);
            Assert.NotNull(slot0State.Result.itemUsable);

            var equipment = (Equipment)slot0State.Result.itemUsable;
            var additionalStats = equipment.StatsMap
                .GetAdditionalStats(true)
                .ToArray();
            var skills = equipment.Skills;
            Assert.Equal(optionNumbers.Length, equipment.optionCountFromCombination);

            var optionSheet = _tableSheets.EquipmentItemOptionSheet;
            var mainAdditionalStatMin = 0;
            var mainAdditionalStatMax = 0;
            var requiredBlockIndex = recipeRow.RequiredBlockIndex + subRecipeRow.RequiredBlockIndex;
            var orderedOptions = subRecipeRow.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id)
                .ToArray();
            foreach (var optionNumber in optionNumbers)
            {
                var optionInfo = orderedOptions[optionNumber - 1];
                requiredBlockIndex += optionInfo.RequiredBlockIndex;
                var optionRow = optionSheet[optionInfo.Id];
                if (optionRow.StatMin > 0 || optionRow.StatMax > 0)
                {
                    if (optionRow.StatType == equipment.UniqueStatType)
                    {
                        mainAdditionalStatMin += optionRow.StatMin;
                        mainAdditionalStatMax += optionRow.StatMax;
                        continue;
                    }

                    var additionalStatValue = additionalStats
                        .First(e => e.statType == optionRow.StatType)
                        .additionalValue;
                    Assert.True(additionalStatValue >= optionRow.StatMin);
                    Assert.True(additionalStatValue <= optionRow.StatMax + 1);
                }
                else if (optionRow.SkillId != default)
                {
                    var skill = skills.First(e => e.SkillRow.Id == optionRow.SkillId);
                    Assert.True(skill.Chance >= optionRow.SkillChanceMin);
                    Assert.True(skill.Chance <= optionRow.SkillChanceMax + 1);
                    Assert.True(skill.Power >= optionRow.SkillDamageMin);
                    Assert.True(skill.Power <= optionRow.SkillDamageMax + 1);
                }
            }

            var mainAdditionalStatValue = additionalStats
                .First(e => e.statType == equipment.UniqueStatType)
                .additionalValue;
            Assert.True(mainAdditionalStatValue >= mainAdditionalStatMin);
            Assert.True(mainAdditionalStatValue <= mainAdditionalStatMax + 1);
            Assert.Equal(requiredBlockIndex, slot0State.RequiredBlockIndex);

            if (requiredBlockIndex == 0)
            {
                return;
            }

            var hourglassRow = _tableSheets.MaterialItemSheet
                .First(pair => pair.Value.ItemSubType == ItemSubType.Hourglass)
                .Value;

            inventoryValue = nextState.GetState(_inventoryAddress);
            Assert.NotNull(inventoryValue);
            inventoryState = new Inventory((List)inventoryValue);
            Assert.False(inventoryState.TryGetFungibleItems(hourglassRow.ItemId, out _));

            var diff = slot0State.RequiredBlockIndex - GameConfig.RequiredAppraiseBlock;
            var hourglassCount = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            inventoryState.AddFungibleItem(
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, hourglassRow.Id),
                hourglassCount);
            Assert.True(inventoryState.TryGetFungibleItems(hourglassRow.ItemId, out var hourglasses));
            Assert.Equal(hourglassCount, hourglasses.Sum(e => e.count));
            nextState = nextState.SetState(_inventoryAddress, inventoryState.Serialize());

            var rapidCombinationAction = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            nextState = rapidCombinationAction.Execute(new ActionContext
            {
                PreviousStates = nextState,
                BlockIndex = GameConfig.RequiredAppraiseBlock,
                Random = random,
                Signer = _agentAddress,
            });
            inventoryValue = nextState.GetState(_inventoryAddress);
            Assert.NotNull(inventoryValue);
            inventoryState = new Inventory((List)inventoryValue);
            Assert.False(inventoryState.TryGetFungibleItems(hourglassRow.ItemId, out _));
        }
    }
}
