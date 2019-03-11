using System;
using Nekoyume.Action;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Monster : CharacterBase
    {
        public int rewardExp;
        public Data.Table.Monster data;
        public ItemBase item;

        public Monster(Data.Table.Monster data, Player player, ItemBase item)
        {
            hp = data.Health;
            atk = data.Attack;
            def = data.Defense;
            rewardExp = data.RewardExp;
            targets.Add(player);
            simulator = player.simulator;
            this.data = data;
            if (item != null)
            {
                this.item = item;
            }
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) targets[0];
            player.GetExp(this);
            if (item != null)
            {
                var dropItem = new DropItem
                {
                    character = Copy(this),
                    characterId = id,
                };
                simulator.Log.Add(dropItem);
                player.GetItem(item);
            }
        }
    }
}
