namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class HackAndSlashSweepTest3
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

        public HackAndSlashSweepTest3()
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

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level)
                .Select(e => e.NonFungibleId).ToList();
            var random = new TestRandom();
            var costumes = new List<Guid>();
            if (avatarState.level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipments, costumes);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            var worldId = 1;
            var stageId = 1;
            var apStoneCount = 1;
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

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
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
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount = gameConfigState.ActionPointMax / stageRow.CostAP * apStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExpV1(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    playCount);

                var random = new TestRandom(_random.Seed);
                var expectedRewardItems =
                    HackAndSlashSweep3.GetRewardItems(random, playCount, stageRow, _tableSheets.MaterialItemSheet);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep3
                {
                    actionPoint = avatarState.actionPoint,
                    costumes = costumes,
                    equipments = equipments,
                    avatarAddress = _avatarAddress,
                    apStoneCount = apStoneCount,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws<ActionObsoletedException>(() =>
                {
                    action.Execute(new ActionContext
                    {
                        PreviousStates = state,
                        Signer = _agentAddress,
                        Random = _random,
                    });
                });
            }
        }
    }
}
