using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Shoes : Equipment
    {
        public Shoes(Data.Table.Item data, Guid id)
            : base(data, id)
        {
        }
    }
}
