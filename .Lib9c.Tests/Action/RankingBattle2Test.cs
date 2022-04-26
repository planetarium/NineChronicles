namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class RankingBattle2Test
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _weeklyArenaAddress;
        private IAccountStateDelta _initialState;

        public RankingBattle2Test(ITestOutputHelper outputHelper)
        {
            _initialState = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            var rankingMapAddress = new PrivateKey().ToAddress();

            var (agent1State, avatar1State) = RankingBattle11Test.GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            _agent1Address = agent1State.address;
            _avatar1Address = avatar1State.address;

            var (agent2State, avatar2State) = RankingBattle11Test.GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            var agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;

            var weeklyArenaState = new WeeklyArenaState(0);
            weeklyArenaState.SetV2(avatar1State, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar1Address].Activate();
            weeklyArenaState.SetV2(avatar2State, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar2Address].Activate();
            _weeklyArenaAddress = weeklyArenaState.address;

            _initialState = _initialState
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address, avatar1State.Serialize())
                .SetState(agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(_weeklyArenaAddress, weeklyArenaState.Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Execute()
        {
            var previousWeeklyState = _initialState.GetWeeklyArenaState(0);
            var previousAvatar1State = _initialState.GetAvatarState(_avatar1Address);
            previousAvatar1State.level = 10;

            var previousState = _initialState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var itemIds = _tableSheets.WeeklyArenaRewardSheet.Values
                .Select(r => r.Reward.ItemId)
                .ToList();

            Assert.All(itemIds, id => Assert.False(previousAvatar1State.inventory.HasItem(id)));

            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row.CostumeId], new TestRandom());
            costume.equipped = true;
            previousAvatar1State.inventory.AddItem2(costume);

            var row2 = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var enemyCostume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row2.CostumeId], new TestRandom());
            enemyCostume.equipped = true;
            var enemyAvatarState = _initialState.GetAvatarState(_avatar2Address);
            enemyAvatarState.inventory.AddItem2(enemyCostume);

            previousState = previousState
                .SetState(_avatar1Address, previousAvatar1State.Serialize())
                .SetState(_avatar2Address, enemyAvatarState.Serialize());

            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int> { costume.Id },
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Null(action.Result);

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatar1State = nextState.GetAvatarState(_avatar1Address);
            var nextWeeklyState = nextState.GetWeeklyArenaState(0);

            Assert.Contains(nextAvatar1State.inventory.Materials, i => itemIds.Contains(i.Id));
            Assert.NotNull(action.Result);
            Assert.Contains(typeof(GetReward), action.Result.Select(e => e.GetType()));
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
            Assert.True(nextWeeklyState[_avatar1Address].Score >
                        previousWeeklyState[_avatar1Address].Score);
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar1Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<InvalidAddressException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ExecuteThrowFailedLoadStateException(int caseIndex)
        {
            Address signer = default;
            Address avatarAddress = default;
            Address enemyAddress = default;

            switch (caseIndex)
            {
                case 0:
                    signer = new PrivateKey().ToAddress();
                    avatarAddress = _avatar1Address;
                    enemyAddress = _avatar2Address;
                    break;
                case 1:
                    signer = _agent1Address;
                    avatarAddress = _avatar1Address;
                    enemyAddress = new PrivateKey().ToAddress();
                    break;
            }

            var action = new RankingBattle2
            {
                AvatarAddress = avatarAddress,
                EnemyAddress = enemyAddress,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<FailedLoadStateException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = signer,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
        {
            var previousAvatar1State = _initialState.GetAvatarState(_avatar1Address);
            previousAvatar1State.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                false
            );
            var previousState = _initialState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowWeeklyArenaStateAlreadyEndedException()
        {
            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            previousWeeklyArenaState.Ended = true;

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<WeeklyArenaStateAlreadyEndedException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ExecuteThrowWeeklyArenaStateNotContainsAvatarAddressException(
            int caseIndex)
        {
            Address targetAddress = default;
            switch (caseIndex)
            {
                case 0:
                    targetAddress = _avatar1Address;
                    break;
                case 1:
                    targetAddress = _avatar2Address;
                    break;
            }

            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            previousWeeklyArenaState.Remove(targetAddress);

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<WeeklyArenaStateNotContainsAvatarAddressException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowNotEnoughWeeklyArenaChallengeCountException()
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatar1Address);
            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            while (true)
            {
                var arenaInfo = previousWeeklyArenaState.GetArenaInfo(_avatar1Address);
                arenaInfo.UpdateV3(previousAvatarState, arenaInfo, BattleLog.Result.Lose);
                if (arenaInfo.DailyChallengeCount == 0)
                {
                    break;
                }
            }

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Throws<NotEnoughWeeklyArenaChallengeCountException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var action = new RankingBattle2
            {
                AvatarAddress = _avatar1Address,
                EnemyAddress = _avatar2Address,
                WeeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };
            action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (RankingBattle2)formatter.Deserialize(ms);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }
    }
}
