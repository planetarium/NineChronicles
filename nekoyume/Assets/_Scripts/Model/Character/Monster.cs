using System;
using System.Runtime.CompilerServices;
using Nekoyume.Action;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Trigger;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public Data.Table.Character data;

        public Monster(Character data, int monsterLevel, Player player)
        {
            hp = data.hp;
            atk = data.damage;
            def = data.defense;
            criticalChance = data.luck;
            targets.Add(player);
            Simulator = player.Simulator;
            this.data = data;
            level = monsterLevel;
            if (monsterLevel > 1)
            {
                hp += data.lvHp * monsterLevel;
                atk += data.lvDamage * monsterLevel;
                def += data.lvDefense * monsterLevel;
            }
            defElement = Game.Elemental.Create(data.elemental);
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
        }
    }
}
