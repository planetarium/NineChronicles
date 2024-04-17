using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class RankingSimulatorTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [Test]
        public void SerializeBattleLog()
        {
            var agentState = new AgentState(new Address());
            var avatarState = new AvatarState(
                new Address(),
                agentState.address,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            var arenaInfo = new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false);
            var simulator = new RankingSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                999999
            );
            simulator.Simulate();
            var rewards = RewardSelector.Select(
                simulator.Random,
                _tableSheets.WeeklyArenaRewardSheet,
                _tableSheets.MaterialItemSheet,
                avatarState.level,
                arenaInfo.GetRewardCount());
            var challengerScoreDelta = arenaInfo.Update(
                arenaInfo,
                simulator.Result,
                ArenaScoreHelper.GetScore);
            simulator.PostSimulate(rewards, challengerScoreDelta, arenaInfo.Score);

            void ToBytes()
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, simulator.Log);
                    stream.ToArray();
                }
            }

            Assert.DoesNotThrow(ToBytes);
        }
    }
}
