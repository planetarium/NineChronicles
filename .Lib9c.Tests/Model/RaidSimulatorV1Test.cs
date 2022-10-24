namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Xunit;

    public class RaidSimulatorV1Test
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public RaidSimulatorV1Test()
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

            _avatarState.level = 250;
        }

        [Fact]
        public void Simulate()
        {
            var bossId = _tableSheets.WorldBossListSheet.First().Value.BossId;
            var simulator = new RaidSimulatorV1(
                bossId,
                _random,
                _avatarState,
                new List<Guid>(),
                _tableSheets.GetRaidSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet);
            Assert.Equal(_random, simulator.Random);

            var log = simulator.Simulate();

            var turn = log.OfType<WaveTurnEnd>().Count();
            Assert.Equal(simulator.TurnNumber, turn);

            var expectedWaveCount = _tableSheets.WorldBossCharacterSheet[bossId].WaveStats.Count;
            Assert.Equal(expectedWaveCount, log.waveCount);

            var deadEvents = log.OfType<Dead>();
            foreach (var dead in deadEvents)
            {
                Assert.True(dead.Character.IsDead);
            }
        }
    }
}
