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
        }

        protected override void OnDead()
        {
            var player = (Player) targets[0];
            player.GetExp(rewardExp);
        }
    }
}
