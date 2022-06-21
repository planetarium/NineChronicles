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
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CombinationEquipment11Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly IAccountStateDelta _initialState;

        public CombinationEquipment11Test(ITestOutputHelper outputHelper)
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

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

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
        [InlineData(true)]
        [InlineData(false)]
        private void Execute(bool backward)
        {
            var currency = new Currency("NCG", 2, minter: null);
            var row = _tableSheets.EquipmentItemRecipeSheet[2];
            var requiredStage = row.UnlockStage;
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

            previousState = previousState.MintAsset(_agentAddress, 2 * currency);

            var action = new CombinationEquipment11
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = 2,
                subRecipeId = 3,
            };

            Assert.Throws<ActionObsoletedException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = previousState,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });
            });
        }
    }
}
