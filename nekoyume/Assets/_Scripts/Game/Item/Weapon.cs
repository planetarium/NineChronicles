using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        public Weapon(Data.Table.Item data)
            : base(data)
        {
            
        }

//        public override string ToItemInfo()
//        {
//            return $"공격력 +{Data.param0}";
//        }

        public override void UpdatePlayer(Player player)
        {
//            player.atk += Data.param0;
        }
    }
}
