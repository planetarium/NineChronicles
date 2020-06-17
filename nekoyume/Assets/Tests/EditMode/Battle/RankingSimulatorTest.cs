using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
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
            var avatarState = new AvatarState(new Address(), agentState.address, 0, _tableSheets, new GameConfigState());
            var simulator = new RankingSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets
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
