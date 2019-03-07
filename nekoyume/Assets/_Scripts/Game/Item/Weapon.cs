using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        public Weapon(Data.Table.Item data)
            : base(data)
        {
            
        }

        public override string ToItemInfo()
        {
            return $"공격력 +{Data.Param_0}";
        }
    }
}
