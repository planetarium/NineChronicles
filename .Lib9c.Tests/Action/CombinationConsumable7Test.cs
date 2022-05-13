namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CombinationConsumable7Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialState;

        public CombinationConsumable7Test()
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

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow, _random);
                avatarState.inventory.AddItem(material, count: materialInfo.Count);
            }

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            var equipment = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.First, default, 0);

            var result = new CombinationConsumable5.ResultModel
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
                avatarState.Update(mail);
            }

            var previousState = _initialState.SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize());
            if (backward)
            {
                previousState = previousState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                previousState = previousState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            var action = new CombinationConsumable7
            {
                AvatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);
        }
    }
}
