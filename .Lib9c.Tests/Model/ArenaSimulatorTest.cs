namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus.Arena;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState1;
        private readonly AvatarState _avatarState2;

        private readonly ArenaAvatarState _arenaAvatarState1;
        private readonly ArenaAvatarState _arenaAvatarState2;

        public ArenaSimulatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState1 = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            _avatarState2 = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            _arenaAvatarState1 = new ArenaAvatarState(_avatarState1);
            _arenaAvatarState2 = new ArenaAvatarState(_avatarState2);
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new ArenaSimulator(_random);
            var myDigest = new ArenaPlayerDigest(_avatarState1, _arenaAvatarState1);
            var enemyDigest = new ArenaPlayerDigest(_avatarState2, _arenaAvatarState2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var log = simulator.Simulate(myDigest, enemyDigest, arenaSheets);

            Assert.Equal(_random, simulator.Random);

            var turn = log.Events.OfType<ArenaTurnEnd>().Count();
            Assert.Equal(simulator.Turn, turn);

            var players = log.Events.OfType<ArenaSpawnCharacter>();
            var arenaCharacters = new List<ArenaCharacter>();
            foreach (var player in players)
            {
                if (player.Character is ArenaCharacter arenaCharacter)
                {
                    arenaCharacters.Add(arenaCharacter);
                }
            }

            Assert.Equal(2, players.Count());
            Assert.Equal(2, arenaCharacters.Count);
            Assert.Equal(1, arenaCharacters.Count(x => x.IsEnemy));
            Assert.Equal(1, arenaCharacters.Count(x => !x.IsEnemy));

            var dead = log.Events.OfType<ArenaDead>();
            Assert.Single(dead);
            var deadCharacter = dead.First().Character;
            Assert.True(deadCharacter.IsDead);
            Assert.Equal(0, deadCharacter.CurrentHP);
            if (log.Result == ArenaLog.ArenaResult.Win)
            {
                Assert.True(deadCharacter.IsEnemy);
            }
            else
            {
                Assert.False(deadCharacter.IsEnemy);
            }
        }
    }
}
