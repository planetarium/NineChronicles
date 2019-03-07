using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Material : ItemBase
    {
        public Material(Data.Table.Item data) : base(data)
        {
        }

        public override string ToItemInfo()
        {
            return "";
        }
    }
}
