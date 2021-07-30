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
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CombinationEquipmentTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;
        private IAccountStateDelta _initialState;

        public CombinationEquipmentTest(ITestOutputHelper outputHelper)
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

            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.SerializeV2())
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
            var action = new CombinationEquipment
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
            CombinationEquipment.AddAndUnlockOption(
                agentState,
                equipment,
                _random,
                subRecipe,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(equipment.optionCountFromCombination > 0);
        }

        private void Execute(bool backward, int recipeId, int? subRecipeId, int mintNCG)
        {
            var currency = new Currency("NCG", 2, minter: null);
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var requiredStage = row.UnlockStage;
            var costActionPoint = row.RequiredActionPoint;
            var costNCG = row.RequiredGold * currency;
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem(material, count: row.MaterialCount);

            if (subRecipeId.HasValue)
            {
                var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                costActionPoint += subRow.RequiredActionPoint;
                costNCG += subRow.RequiredGold * currency;

                foreach (var materialInfo in subRow.Materials)
                {
                    material = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                    _avatarState.inventory.AddItem(material, count: materialInfo.Count);
                }
            }

            _avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                requiredStage);

            var equipmentRow = _tableSheets.EquipmentItemSheet[row.ResultEquipmentId];
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);

            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = costNCG.RawValue,
                actionPoint = costActionPoint,
                recipeId = recipeId,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                subRecipeId = subRecipeId,
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.UpdateV4(mail, 0);
            }

            if (backward)
            {
                _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                _initialState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize())
                    .SetState(_avatarAddress, _avatarState.SerializeV2());
            }

            var previousActionPoint = backward
                ? _initialState.GetAvatarState(_avatarAddress).actionPoint
                : _initialState.GetAvatarStateV2(_avatarAddress).actionPoint;

            _initialState = _initialState.MintAsset(_agentAddress, mintNCG * currency);
            var goldCurrencyState = _initialState.GetGoldCurrency();
            var previousNCG = _initialState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(mintNCG * currency, previousNCG);
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
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
            Assert.Equal(1, nextAvatarState.mailBox.Count);

            var agentGold = nextState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(previousNCG - costNCG, agentGold);

            var blackSmithGold = nextState.GetBalance(Addresses.Blacksmith, goldCurrencyState);
            Assert.Equal(costNCG, blackSmithGold);
        }
    }
}
