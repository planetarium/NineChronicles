namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombatTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Player _player;
        private readonly Enemy _enemy;

        public CombatTest()
        {
            var csv = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(csv);

            var gameConfigState = new GameConfigState(csv[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default);

            _player = new Player(
                level: 1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new TestSimulator(
                new TestRandom(),
                avatarState,
                new List<System.Guid>(),
                _tableSheets.GetSimulatorSheets());
            _player.Simulator = simulator;

            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            _enemy = new Enemy(
                _player,
                new CharacterStats(enemyRow, 1),
                enemyRow.Id,
                ElementalType.Normal);
            _enemy.Targets.Add(_player);
        }

        [Theory]
        [InlineData(3, 7, 1000, 110, 90)]
        [InlineData(7, 3, 1000, 110, 90)]
        [InlineData(10, 10, 5000, 120, 50)]
        [InlineData(0, 1000, 0, 999, 1)]
        [InlineData(1000, 0, 0, 999, 1)]
        [InlineData(0, 0, 10000, int.MaxValue, 1)]
        public void CalculateDEFAndDamageReduction(int def, int drv, int drr, int enemyATK, int expectedDamage)
        {
            _player.Stats.SetStatForTest(StatType.DEF, def);
            _player.Stats.SetStatForTest(StatType.DRV, drv);
            _player.Stats.SetStatForTest(StatType.DRR, drr);
            _player.Stats.SetStatForTest(StatType.HIT, 0);
            _enemy.Stats.SetStatForTest(StatType.ATK, enemyATK);
            _enemy.Stats.SetStatForTest(StatType.CRI, 0);

            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 0, 100);

            var prevHP = _player.CurrentHP;
            normalAttack.Use(_enemy, 1, new List<StatBuff>());
            var currentHP = _player.CurrentHP;
            var damage = prevHP - currentHP;

            Assert.Equal(expectedDamage, damage);
        }

        [Theory]
        [InlineData(10000, 10, 25)]
        [InlineData(15000, 10, 30)]
        [InlineData(0, 10, 15)]
        [InlineData(-4000, 10, 11)]
        [InlineData(int.MinValue, 10, 10)]
        public void CalculateCritDamage(int cdmg, int atk, int expectedDamage)
        {
            _player.Stats.SetStatForTest(StatType.DEF, 0);
            _player.Stats.SetStatForTest(StatType.HIT, 0);
            _enemy.Stats.SetStatForTest(StatType.ATK, atk);
            _enemy.Stats.SetStatForTest(StatType.CDMG, cdmg);
            _enemy.Stats.SetStatForTest(StatType.CRI, 100);

            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 0, 100);

            var prevHP = _player.CurrentHP;
            normalAttack.Use(_enemy, 1, new List<StatBuff>());
            var currentHP = _player.CurrentHP;
            var damage = prevHP - currentHP;

            Assert.Equal(expectedDamage, damage);
        }
    }
}
