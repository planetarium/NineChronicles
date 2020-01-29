using System;
using System.Linq;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class Enemy : CharacterBase, ICloneable
    {
        public int spawnIndex = -1;

        public Enemy(CharacterBase player, CharacterSheet.Row rowData, int monsterLevel) : base(player.Simulator, rowData.Id, monsterLevel)
        {
            Targets.Add(player);
        }

        protected Enemy(Enemy value) : base(value)
        {
            spawnIndex = value.spawnIndex;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) Targets[0];
            player.RemoveTarget(this);
        }

        protected sealed override void SetSkill()
        {
            base.SetSkill();

            var dmg = (int) (ATK * 0.3m);
            var skillIds = Simulator.TableSheets.EnemySkillSheet.Values.Where(r => r.characterId == RowData.Id)
                .Select(r => r.skillId).ToList();
            var enemySkills = Simulator.TableSheets.SkillSheet.Values.Where(r => skillIds.Contains(r.Id))
                .ToList();
            foreach (var skillRow in enemySkills)
            {
                var skill = SkillFactory.Get(skillRow, dmg, 100);
                Skills.Add(skill);
            }
        }

        public override object Clone()
        {
            return new Enemy(this);
        }
    }
}
