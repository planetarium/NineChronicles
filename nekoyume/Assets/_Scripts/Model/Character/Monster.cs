using System;
using Nekoyume.Game;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public CharacterSheet.Row data;
        public sealed override float TurnSpeed { get; set; }

        public int spawnIndex = -1; 

        public Monster(CharacterSheet.Row data, int monsterLevel, Player player) : base(player.Simulator)
        {
            var stats = data.ToStats(monsterLevel);
            currentHP = stats.HP;
            atk = stats.Damage;
            def = stats.Defense;
            luck = stats.Luck;
            targets.Add(player);
            this.data = data;
            level = monsterLevel;
            atkElement = Game.Elemental.Create(data.Elemental);
            defElement = Game.Elemental.Create(data.Elemental);
            TurnSpeed = 1.0f;
            attackRange = data.AttackRange;
            hp = stats.HP;
            runSpeed = data.RunSpeed;
            characterSize = data.Size;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.RemoveTarget(this);
        }

        protected sealed override void SetSkill()
        {
            base.SetSkill();
            //TODO 몬스터별 스킬 구현
            var dmg = (int) (atk * 1.3m);
            var chance = .1m;
            foreach (var skillRow in Game.Game.instance.TableSheets.SkillSheet)
            {
                var skill = SkillFactory.Get(skillRow, dmg, chance);
                Skills.Add(skill);
            }
        }
    }
}
