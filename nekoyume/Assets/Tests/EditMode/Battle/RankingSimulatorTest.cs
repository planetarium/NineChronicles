using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
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
                new GameConfigState(),
                default
            );
            var arenaInfo = new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false);
            var simulator = new RankingSimulatorV1(
                new Cheat.DebugRandom(),
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheets(),
                999999,
                arenaInfo,
                arenaInfo
            );
            simulator.Simulate();

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
