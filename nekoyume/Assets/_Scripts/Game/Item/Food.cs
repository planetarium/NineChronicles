using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Food : ItemUsable
    {
        public Food(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
