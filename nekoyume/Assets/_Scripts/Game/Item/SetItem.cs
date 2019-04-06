using System;
using System.Collections.Generic;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class SetItem : Equipment
    {
        public SetItem(Data.Table.Item data) : base(data)
        {
        }

//        public override string ToItemInfo() => $"공격력 +{Data.param0}\n방어력 +{Data.param1}";

        public override void UpdatePlayer(Player player)
        {
//            player.atk += Data.param0;
//            player.def += Data.param1;
        }

        public static Dictionary<int, int> WeaponMap =>
            new Dictionary<int, int>
            {
                [308001] = 301001,
                [308002] = 301002,
                [308003] = 301003,
            };
    }
}
