using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(Data.Table.Item data, Guid id)
            : base(data, id)
        {
        }
    }
}
