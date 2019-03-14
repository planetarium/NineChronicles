using System;
using System.Text;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class SetItem : Equipment
    {
        public SetItem(Data.Table.Item data) : base(data)
        {
        }

        public override string ToItemInfo() => $"공격력 +{Data.param0}\n방어력 +{Data.param1}";

        public override void UpdatePlayer(Player player)
        {
            player.atk += Data.param0;
            player.def += Data.param1;
        }
    }
}
