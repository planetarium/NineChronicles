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

        public override string ToItemInfo()
        {
            return $"방어력 +{Data.Param_0}";
        }

        public override void UpdatePlayer(Player player)
        {
            player.def += Data.Param_0;
        }
    }
}
