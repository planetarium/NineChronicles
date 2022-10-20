namespace Lib9c.Tests.Action
{
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
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CombinationConsumableTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public CombinationConsumableTest()
        {
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
                        GameConfig.RequireClearedStageLevel.CombinationConsumableAction).Serialize())
                .SetState(GameConfigState.Address, gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            var costActionPoint = row.RequiredActionPoint;
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow, _random);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            var previousActionPoint = avatarState.actionPoint;
            var previousResultConsumableCount =
                avatarState.inventory.Equipments.Count(e => e.Id == row.ResultConsumableItemId);
            var previousMailCount = avatarState.mailBox.Count;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            IAccountStateDelta previousState;
            if (backward)
            {
                previousState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                previousState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            var action = new CombinationConsumable
            {
                avatarAddress = _avatarAddress,
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

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(previousActionPoint - costActionPoint, nextAvatarState.actionPoint);
            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultConsumableCount + 1,
                nextAvatarState.inventory.Consumables.Count(e => e.Id == row.ResultConsumableItemId));
        }
    }
}
