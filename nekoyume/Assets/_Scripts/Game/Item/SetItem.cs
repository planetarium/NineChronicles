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

        public override string ToItemInfo()
        {
            return new StringBuilder()
                .AppendLine($"공격력 +{Data.Param_0}")
                .AppendLine($"방어력 +{Data.Param_1}")
                .ToString();
        }

        public override void UpdatePlayer(Player player)
        {
            player.atk += Data.Param_0;
            player.def += Data.Param_1;
        }
    }
}
