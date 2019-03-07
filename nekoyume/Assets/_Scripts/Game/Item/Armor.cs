using System;

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
    }
}
