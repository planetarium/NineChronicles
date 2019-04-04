using System;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public Data.Table.Character data;

        public Monster(Data.Table.Character data, Player player)
        {
            hp = data.hp;
            atk = data.damage;
            def = data.defense;
            criticalChance = data.luck;
            targets.Add(player);
            Simulator = player.Simulator;
            this.data = data;
            defElement = Elemental.Create(data.elemental);
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
        }
    }
}
