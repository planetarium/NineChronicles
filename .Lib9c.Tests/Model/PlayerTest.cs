namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class PlayerTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public PlayerTest()
        {
            _tableSheets = TableSheets.FromTableSheetsState(TableSheetsImporter.ImportTableSheets());
            _random = new ItemEnhancementTest.TestRandom();
            _avatarState = new AvatarState(default, default, 0, _tableSheets, new GameConfigState());
        }

        [Fact]
        public void TickAlive()
        {
            var simulator = new StageSimulator(_random, _avatarState, new List<Guid>(), 1, 1, _tableSheets);
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            for (int i = 0; i < 5; i++)
            {
                // 0 ReduceDurationOfBuffs
                // 1 ReduceSkillCooldown
                // 2 UseSkill
                // 3 RemoveBuffs
                // 4 EndTurn
                player.Tick();
            }

            Assert.True(simulator.Log.Any());
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }

        [Fact]
        public void TickDead()
        {
            var simulator = new StageSimulator(_random, _avatarState, new List<Guid>(), 1, 1, _tableSheets);
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            player.CurrentHP = -1;

            Assert.True(player.IsDead);

            // Check IsAlive
            player.Tick();
            // Call EndTurn
            player.Tick();
            Assert.True(simulator.Log.Any());
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }
    }
}
