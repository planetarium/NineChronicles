namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CombinationEquipment10Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly IAccountStateDelta _initialState;

        public CombinationEquipment10Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(sheets);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(
                    slotAddress,
                    new CombinationSlotState(
                        slotAddress,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(false, 1, null)]
        [InlineData(false, 145, 341)]
        [InlineData(false, 145, 342)]
        [InlineData(true, 1, null)]
        [InlineData(true, 145, 341)]
        [InlineData(true, 145, 342)]
        public void Execute_Success(bool backward, int recipeId, int? subRecipeId) =>
            Execute(backward, recipeId, subRecipeId, 10000);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_Throw_InsufficientBalanceException(bool backward)
        {
            var subRecipeId = _tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList
                .First(e => e.RequiredGold > 0)
                .Id;
            var recipeId = _tableSheets.EquipmentItemRecipeSheet.OrderedList
                .First(e => e.SubRecipeIds.Contains(subRecipeId))
                .Id;

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                backward, recipeId, subRecipeId, 0));
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new CombinationEquipment10
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = 1,
                subRecipeId = 255,
            };
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );

            var updatedAddresses = new List<Address>
            {
                _agentAddress,
                _avatarAddress,
                slotAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                Addresses.Blacksmith,
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        [Fact]
        public void AddAndUnlockOption()
        {
            var agentState = _initialState.GetAgentState(_agentAddress);
            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2.Last;
            Assert.NotNull(subRecipe);
            var equipment = (Necklace)ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet[10411000],
                Guid.NewGuid(),
                default);
            Assert.Equal(0, equipment.optionCountFromCombination);
            CombinationEquipment12.AddAndUnlockOption(
                agentState,
                equipment,
                _random,
                subRecipe,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(equipment.optionCountFromCombination > 0);
        }

        [Theory]
        [InlineData(1, false, 375, false)]
        [InlineData(1, false, 374, false)]
        [InlineData(2, true, 3, true)]
        [InlineData(2, true, 2, false)]
        [InlineData(3, false, 6, false)]
        [InlineData(3, false, 5, false)]
        [InlineData(134, true, 313, false)]
        [InlineData(134, true, 314, false)]
        [InlineData(134, true, 315, true)]
        public void MadeWithMimisbrunnrRecipe(
            int recipeId,
            bool isElementalTypeFire,
            int? subRecipeId,
            bool isMadeWithMimisbrunnrRecipe)
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var requiredStage = row.UnlockStage;
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                requiredStage);

            avatarState.inventory.AddItem(material, row.MaterialCount);

            if (subRecipeId.HasValue)
            {
                var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];

                foreach (var materialInfo in subRow.Materials)
                {
                    material = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                    avatarState.inventory.AddItem(material, materialInfo.Count);
                }
            }

            var previousState = _initialState
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

            previousState = previousState.MintAsset(_agentAddress, 10_000 * currency);

            var action = new CombinationEquipment10
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);
            var isMadeWithMimisbrunnrRecipe_considerElementalType =
                isElementalTypeFire &&
                ((Equipment)slotState.Result.itemUsable).MadeWithMimisbrunnrRecipe;
            Assert.Equal(
                isMadeWithMimisbrunnrRecipe,
                isMadeWithMimisbrunnrRecipe_considerElementalType);
            Assert.Equal(
                isMadeWithMimisbrunnrRecipe,
                ((Equipment)slotState.Result.itemUsable).IsMadeWithMimisbrunnrRecipe(
                    _tableSheets.EquipmentItemRecipeSheet,
                    _tableSheets.EquipmentItemSubRecipeSheetV2,
                    _tableSheets.EquipmentItemOptionSheet
                ));
        }

        private void Execute(bool backward, int recipeId, int? subRecipeId, int mintNCG)
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var requiredStage = row.UnlockStage;
            var costActionPoint = row.RequiredActionPoint;
            var costNCG = row.RequiredGold * currency;
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var previousActionPoint = avatarState.actionPoint;
            var previousResultEquipmentCount =
                avatarState.inventory.Equipments.Count(e => e.Id == row.ResultEquipmentId);
            var previousMailCount = avatarState.mailBox.Count;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                requiredStage);

            avatarState.inventory.AddItem(material, row.MaterialCount);

            if (subRecipeId.HasValue)
            {
                var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                costActionPoint += subRow.RequiredActionPoint;
                costNCG += subRow.RequiredGold * currency;

                foreach (var materialInfo in subRow.Materials)
                {
                    material = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                    avatarState.inventory.AddItem(material, materialInfo.Count);
                }
            }

            IAccountStateDelta previousState;
            if (backward)
            {
                previousState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                previousState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            previousState = previousState.MintAsset(_agentAddress, mintNCG * currency);
            var goldCurrencyState = previousState.GetGoldCurrency();
            var previousNCG = previousState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(mintNCG * currency, previousNCG);

            var action = new CombinationEquipment10
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            if (subRecipeId.HasValue)
            {
                Assert.True(((Equipment)slotState.Result.itemUsable).optionCountFromCombination > 0);
            }
            else
            {
                Assert.Equal(0, ((Equipment)slotState.Result.itemUsable).optionCountFromCombination);
            }

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(previousActionPoint - costActionPoint, nextAvatarState.actionPoint);
            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultEquipmentCount + 1,
                nextAvatarState.inventory.Equipments.Count(e => e.Id == row.ResultEquipmentId));

            var agentGold = nextState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(previousNCG - costNCG, agentGold);

            var blackSmithGold = nextState.GetBalance(Addresses.Blacksmith, goldCurrencyState);
            Assert.Equal(costNCG, blackSmithGold);
        }
    }
}
