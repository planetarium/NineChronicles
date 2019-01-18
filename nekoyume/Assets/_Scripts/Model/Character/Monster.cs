namespace Nekoyume.Model
{
    public class Monster : CharacterBase
    {
        public int rewardExp;
        public Monster(Data.Table.Monster data, Player player)
        {
            hp = data.Health;
            atk = data.Attack;
            rewardExp = data.RewardExp;
            targets.Add(player);
            simulator = player.simulator;
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
        }
    }
}
