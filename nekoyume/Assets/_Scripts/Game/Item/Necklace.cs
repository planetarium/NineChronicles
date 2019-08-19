using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
