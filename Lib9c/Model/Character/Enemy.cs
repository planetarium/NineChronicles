using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class Enemy : CharacterBase, ICloneable
    {
        public int spawnIndex = -1;

        public Enemy(
            CharacterBase player,
            CharacterSheet.Row rowData,
            int monsterLevel,
            IEnumerable<StatModifier> optionalStatModifiers = null)
            : base(
                player.Simulator,
                player.Simulator.CharacterSheet,
                rowData.Id,
                monsterLevel,
                optionalStatModifiers)
        {
            Targets.Add(player);
            PostConstruction();
        }

        public Enemy(Enemy value) : base(value)
        {
            spawnIndex = value.spawnIndex;
            PostConstruction();
        }

        public Enemy(CharacterSheet.Row rowData) : base(rowData)
        {
        }

        private void PostConstruction()
        {
            AttackCountMax = 1;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player)Targets[0];
            player.RemoveTarget(this);
        }

        protected sealed override void SetSkill()
        {
            base.SetSkill();

            var dmg = (int)(ATK * 0.3m);
            var skillIds = GetSkillIds();
            var enemySkills = Simulator.SkillSheet.Values.Where(r => skillIds.Contains(r.Id))
                .ToList();
            foreach (var skillRow in enemySkills)
            {
                var skill = SkillFactory.Get(skillRow, dmg, 100);
                Skills.Add(skill);
            }
        }

        private List<int> GetSkillIds() => Simulator switch
        {
            StageSimulator stageSimulator => stageSimulator.EnemySkillSheet.OrderedList
                .Where(r => r.characterId == RowData.Id)
                .Select(r => r.skillId)
                .ToList(),
            EventDungeonBattleSimulator eventDungeonBattleSimulator => eventDungeonBattleSimulator.EnemySkillSheet
                .OrderedList.Where(r => r.characterId == RowData.Id)
                .Select(r => r.skillId)
                .ToList(),
            _ => new List<int>()
        };

        public override object Clone()
        {
            return new Enemy(this);
        }
    }
}
