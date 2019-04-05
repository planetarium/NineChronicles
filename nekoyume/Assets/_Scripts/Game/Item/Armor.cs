using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(Data.Table.Item data) : base(data)
        {
        }

//        public override string ToItemInfo()
//        {
//            return $"방어력 +{Data.param0}";
//        }

        public override void UpdatePlayer(Player player)
        {
//            player.def += Data.param0;
        }
    }
}
