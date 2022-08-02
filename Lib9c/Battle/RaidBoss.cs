using Nekoyume.Battle;
using Nekoyume.Model.Character;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nekoyume.Model
{
    public class RaidBoss : Enemy
    {
        [NonSerialized]
        public RaidSimulator RaidSimulator;

        public new WorldBossCharacterSheet.Row RowData { get; }

        public RaidBoss(
            CharacterBase player,
            WorldBossCharacterSheet.Row row,
            WorldBossCharacterSheet.WaveStatData statData)
            : base(
                player,
                new CharacterStats(statData),
                row.BossId,
                statData.ElementalType)
        {
            RaidSimulator = (RaidSimulator) player.Simulator;
            RowData = row;
        }

        public RaidBoss(RaidBoss value) : base(value)
        {
            RaidSimulator = value.RaidSimulator;
            RowData = value.RowData;
        }

        public override object Clone() => new RaidBoss(this);

        protected override void SetSkill()
        {
            if (!Simulator.SkillSheet.TryGetValue(GameConfig.DefaultAttackId, out var normalAttackRow))
            {
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
            }

            var attack = SkillFactory.Get(normalAttackRow, 0, 100);
            Skills.Add(attack);

            var dmg = (int)(ATK * 0.3m);
            var skillIds = RaidSimulator.EnemySkillSheet.Values.Where(r => r.characterId == RowData.BossId)
                .Select(r => r.skillId).ToList();
            var enemySkills = Simulator.SkillSheet.Values.Where(r => skillIds.Contains(r.Id))
                .ToList();
            foreach (var skillRow in enemySkills)
            {
                var skill = SkillFactory.Get(skillRow, dmg, 100);
                Skills.Add(skill);
            }
        }
    }
}
