using System;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public int rewardExp;
        public Data.Table.Monster data;

        public Monster(Data.Table.Monster data, Player player)
        {
            hp = data.Health;
            atk = data.Attack;
            def = data.Defense;
            rewardExp = data.RewardExp;
            criticalChance = data.critical;
            targets.Add(player);
            Simulator = player.Simulator;
            this.data = data;
            defElement = Elemental.Create(data.Resistance);
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
        }
    }
}
