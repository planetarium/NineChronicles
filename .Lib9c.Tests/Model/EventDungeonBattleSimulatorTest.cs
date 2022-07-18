namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.State;
    using Xunit;

    public class EventDungeonBattleSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public EventDungeonBattleSimulatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();
            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new EventDungeonBattleSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                10010001,
                10010001,
                _tableSheets.GetEventDungeonBattleSimulatorSheets(),
                2,
                false,
                10,
                1);

            var player = simulator.Player;
            while (player.Level == 1)
            {
                simulator.Simulate(1);
            }

            Assert.Equal(2, player.Level);
            Assert.True(simulator.Log.OfType<GetExp>().Any());
        }
    }
}
