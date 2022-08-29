namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Xunit;

    public class CombinationEquipment2Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;
        private IAccountStateDelta _initialState;

        public CombinationEquipment2Test()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive("avatar");
            _slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(_sheets);
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
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(
                    _slotAddress,
                    new CombinationSlotState(
                        _slotAddress,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
                    ).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in _sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update2(mail);
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment2()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);

            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);

            Assert.Equal(30, nextAvatarState.mailBox.Count);
        }
    }
}
