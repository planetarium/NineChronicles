namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.GrandFinale;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.GrandFinale;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BattleGrandFinale1Test
    {
        private readonly int _validSeason;
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agent1Address;
        private readonly Address _agent2Address;
        private readonly Address _agent3Address;
        private readonly Address _agent4Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _avatar3Address;
        private readonly Address _avatar4Address;
        private IAccountStateDelta _initialStates;

        public BattleGrandFinale1Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialStates = new State();

            _sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in _sheets)
            {
                _initialStates = _initialStates.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(ncg);

            var rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            // account 1
            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);

            _agent1Address = agent1State.address;
            _avatar1Address = avatar1State.address;
            var arenaAvatarState1 =
                _initialStates.GetArenaAvatarState(
                    ArenaAvatarState.DeriveAddress(_avatar1Address),
                    avatar1State);

            // account 2
            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);
            _agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;
            var arenaAvatarState2 =
                _initialStates.GetArenaAvatarState(
                    ArenaAvatarState.DeriveAddress(_avatar2Address),
                    avatar2State);

            // account 3
            var (agent3State, avatar3State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);
            _agent3Address = agent3State.address;
            _avatar3Address = avatar3State.address;
            var arenaAvatarState3 =
                _initialStates.GetArenaAvatarState(
                    ArenaAvatarState.DeriveAddress(_avatar3Address),
                    avatar3State);

            // account 4
            var (agent4State, avatar4State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);
            var arenaAvatarState4 =
                _initialStates.GetArenaAvatarState(
                    ArenaAvatarState.DeriveAddress(_avatar4Address),
                    avatar4State);

            // update GrandFinaleParticipantsSheet
            _validSeason = _tableSheets.GrandFinaleParticipantsSheet.First!.Key;
            var participantsSheetCsv = _sheets[nameof(GrandFinaleParticipantsSheet)];
            participantsSheetCsv =
                new[] { _avatar1Address, _avatar2Address, _avatar3Address, _avatar4Address, }
                    .Aggregate(
                        participantsSheetCsv,
                        (current, avatarAddr) =>
                            current + $"{_validSeason},{avatarAddr.ToString()}\n");
            _tableSheets.GrandFinaleParticipantsSheet.Set(participantsSheetCsv);
            _agent4Address = agent4State.address;
            _avatar4Address = avatar4State.address;

            _initialStates = _initialStates
                .SetState(
                    Addresses.GetSheetAddress<GrandFinaleParticipantsSheet>(),
                    _tableSheets.GrandFinaleParticipantsSheet.Serialize())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyInventoryKey),
                    avatar1State.inventory.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyWorldInformationKey),
                    avatar1State.worldInformation.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyQuestListKey),
                    avatar1State.questList.Serialize())
                .SetState(_avatar1Address, avatar1State.SerializeV2())
                .SetState(arenaAvatarState1.Address, arenaAvatarState1.Serialize())
                .SetState(_agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(arenaAvatarState2.Address, arenaAvatarState2.Serialize())
                .SetState(_agent3Address, agent3State.Serialize())
                .SetState(_avatar3Address, avatar3State.Serialize())
                .SetState(arenaAvatarState3.Address, arenaAvatarState3.Serialize())
                .SetState(_agent4Address, agent4State.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyInventoryKey),
                    avatar4State.inventory.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyWorldInformationKey),
                    avatar4State.worldInformation.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyQuestListKey),
                    avatar4State.questList.Serialize())
                .SetState(_avatar4Address, avatar4State.SerializeV2())
                .SetState(arenaAvatarState4.Address, arenaAvatarState4.Serialize())
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Theory]
        [InlineData(1, 2, true)]
        [InlineData(1, 2, false)]
        public void Execute_Success(
            long nextBlockIndex,
            int randomSeed,
            bool win)
        {
            Execute(
                nextBlockIndex,
                _validSeason,
                randomSeed,
                _avatar1Address,
                _avatar2Address,
                win);
        }

        [Fact]
        public void Execute_AlreadyFoughtAvatarException()
        {
            var action = new BattleGrandFinale1
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                grandFinaleId = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var next = action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 1,
            });
            Assert.Throws<AlreadyFoughtAvatarException>(() => action.Execute(new ActionContext
            {
                PreviousStates = next,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 2,
            }));
        }

        [Fact]
        public void Execute_InvalidAddressException()
        {
            var action = new BattleGrandFinale1
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar1Address,
                grandFinaleId = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new BattleGrandFinale1
            {
                myAvatarAddress = _avatar2Address,
                enemyAvatarAddress = _avatar1Address,
                grandFinaleId = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_SheetRowNotFoundException()
        {
            var action = new BattleGrandFinale1
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                grandFinaleId = 9999999,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ThisArenaIsClosedException()
        {
            var blockIndex =
                _tableSheets.GrandFinaleScheduleSheet.GetRowByBlockIndex(0)?.StartBlockIndex - 1 ??
                0;
            var action = new BattleGrandFinale1
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                grandFinaleId = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<ThisArenaIsClosedException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = blockIndex,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_AddressNotFoundInArenaParticipantsException(bool excludeMe)
        {
            const int grandFinaleId = 1;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<GrandFinaleScheduleSheet>().TryGetValue(
                grandFinaleId,
                out var row));

            var (tempAgent, tempAvatar) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                new PrivateKey().ToAddress(),
                1);
            var myAvatar =
                excludeMe ? previousStates.GetAvatarStateV2(_avatar1Address) : tempAvatar;
            var (enemyAgent, enemyAvatar) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                new PrivateKey().ToAddress(),
                1);
            previousStates = previousStates
                .SetState(tempAgent.address, tempAgent.Serialize())
                .SetState(enemyAgent.address, enemyAgent.Serialize())
                .SetState(myAvatar.address, myAvatar.Serialize())
                .SetState(enemyAvatar.address, enemyAvatar.Serialize());

            var action = new BattleGrandFinale1
            {
                myAvatarAddress = myAvatar.address,
                enemyAvatarAddress = enemyAvatar.address,
                grandFinaleId = grandFinaleId,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<AddressNotFoundInArenaParticipantsException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousStates = previousStates,
                    Signer = myAvatar.agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = row.StartBlockIndex,
                }));
        }

        private static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress,
            int clearStageId)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    clearStageId),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);

            return (agentState, avatarState);
        }

        private void Execute(long blockIndex, int grandFinaleId, int randomSeed, Address myAvatarAddr, Address enemyAvatarAddr, bool setToWin)
        {
            var states = _initialStates;
            if (!states.TryGetArenaAvatarState(
                    ArenaAvatarState.DeriveAddress(myAvatarAddr),
                    out var myArenaAvatar))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"my avatar not has arena avatar state...: {myAvatarAddr}");
            }

            var myAvatar = states.GetAvatarState(myAvatarAddr);
            var myAgentAddr = myAvatar.agentAddress;
            var participantsCount = states
                .GetSheet<GrandFinaleParticipantsSheet>()[grandFinaleId]
                .Participants.Count;
            if (setToWin)
            {
                myAvatar.level = 100;
                states = states.SetState(myAvatar.address, myAvatar.SerializeV2())
                    .SetState(myArenaAvatar.Address, new ArenaAvatarState(myAvatar).Serialize());
            }
            else
            {
                var enemyAvatar = states.GetAvatarState(enemyAvatarAddr);
                enemyAvatar.level = 100;
                states = states.SetState(enemyAvatar.address, enemyAvatar.SerializeV2())
                    .SetState(
                        ArenaAvatarState.DeriveAddress(enemyAvatar.address),
                        new ArenaAvatarState(enemyAvatar).Serialize());
            }

            var action = new BattleGrandFinale1
            {
                myAvatarAddress = myAvatarAddr,
                enemyAvatarAddress = enemyAvatarAddr,
                grandFinaleId = grandFinaleId,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };
            var nextStates = action.Execute(new ActionContext
            {
                Signer = myAgentAddr,
                BlockIndex = blockIndex,
                PreviousStates = states,
                Random = new TestRandom(randomSeed),
            });
            Assert.True(nextStates.TryGetState<Integer>(
                myAvatarAddr.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        BattleGrandFinale.ScoreDeriveKey,
                        grandFinaleId)),
                out var myScore));
            Assert.Equal<Integer>(setToWin ? 1020 : 1001, myScore);
            Assert.True(nextStates.TryGetState<List>(
                GrandFinaleInformation.DeriveAddress(
                    myAvatarAddr,
                    grandFinaleId),
                out var serialized));
            var nextInformation = new GrandFinaleInformation(serialized);
            Assert.True(nextInformation.TryGetBattleRecord(enemyAvatarAddr, out var win));
            Assert.Equal(setToWin, win);
        }
    }
}
