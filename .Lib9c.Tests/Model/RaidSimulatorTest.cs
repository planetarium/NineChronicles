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

    public class RaidSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public RaidSimulatorTest()
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

            _avatarState.level = 200;
        }

        [Fact]
        public void Simulate()
        {
            var bossId = _tableSheets.WorldBossListSheet.First().Key;
            var simulator = new RaidSimulator(
                bossId,
                _random,
                _avatarState,
                new List<Guid>(),
                _tableSheets.GetRaidSimulatorSheets());

            simulator.Simulate();
            Assert.NotEqual(0, simulator.DamageDealt);
        }
    }
}
