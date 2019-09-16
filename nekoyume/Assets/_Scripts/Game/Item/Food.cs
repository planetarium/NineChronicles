using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Food : ItemUsable
    {
        public Food(Data.Table.Item data, Guid id)
            : base(data, id)
        {
        }
    }
}
