using System;

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
            rewardExp = data.RewardExp;
            targets.Add(player);
            simulator = player.simulator;
            this.data = data;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
        }
    }
}
