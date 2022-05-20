namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class HackAndSlashRandomBuffTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _inventoryAddress;
        private readonly Address _worldInformationAddress;
        private readonly Address _questListAddress;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;
        private readonly IRandom _random;
        private readonly Currency _currency;

        public HackAndSlashRandomBuffTest()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _currency = CrystalCalculator.CRYSTAL;
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress
            )
            {
                level = 100,
            };
            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, agentState.SerializeV2())
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_inventoryAddress, _avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, _avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, _avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            foreach (var address in _avatarState.combinationSlotAddresses)
            {
                var slotState = new CombinationSlotState(
                    address,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                _initialState = _initialState.SetState(address, slotState.Serialize());
            }
        }

        [Theory]
        [InlineData(10, 5, 10_000, 10_000, null)]
        [InlineData(20, 10, 10_000, 10_000, null)]
        [InlineData(20, 10, 10_000, 0, typeof(NotEnoughGatheredStarException))]
        [InlineData(20, 1, 10_000, 10_000, typeof(InvalidGachaCountException))]
        [InlineData(20, 5, 1, 10_000, typeof(NotEnoughFungibleAssetValueException))]
        public void Execute(int stageId, int gachaCount, int balance, int gatheredStar, Type excType)
        {
            var states = _initialState.MintAsset(_agentAddress, balance * _currency);
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), stageId),
                level = 400,
            };
            var gachaStateAddress = _avatarAddress.Derive(HackAndSlashBuffStateKey);
            var gachaState = new HackAndSlashBuffState(stageId);
            states = states
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());
            var crystalStageSheet = _tableSheets.CrystalStageBuffGachaSheet;
            gachaState.Update(gatheredStar, crystalStageSheet);
            states = states.SetState(gachaStateAddress, gachaState.Serialize());
            var cost =
                CrystalCalculator.CalculateBuffGachaCost(stageId, gachaCount, crystalStageSheet);

            var action = new HackAndSlashRandomBuff
            {
                AvatarAddress = _avatarAddress,
                GachaCount = gachaCount,
            };

            if (excType is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    PreviousStates = states,
                    Signer = _agentAddress,
                    Random = _random,
                });

                Assert.Equal(
                    nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL),
                    states.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL) - cost);
            }
            else
            {
                Assert.Throws(excType, () =>
                {
                    action.Execute(new ActionContext
                    {
                        PreviousStates = states,
                        Signer = _agentAddress,
                        Random = _random,
                    });
                });
            }
        }
    }
}
