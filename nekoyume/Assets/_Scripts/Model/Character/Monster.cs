using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public Character data;
        public sealed override float TurnSpeed { get; set; }

        public Monster(Character data, int monsterLevel, Player player)
        {
            var stats = data.GetStats(monsterLevel);
            currentHP = stats.HP;
            atk = stats.Damage;
            def = stats.Defense;
            luck = stats.Luck;
            targets.Add(player);
            Simulator = player.Simulator;
            this.data = data;
            level = monsterLevel;
            ATKElement = Game.Elemental.Create(data.elemental);
            DEFElement = Game.Elemental.Create(data.elemental);
            TurnSpeed = 1.0f;
            attackRange = data.attackRange;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.RemoveTarget(this);
        }

        protected override void SetSkill()
        {
            Skills = new List<SkillBase>();
            var attack = new Game.Skill.Attack(this, targets.First(), atk);
            Skills.Add(attack);
        }
    }
}
