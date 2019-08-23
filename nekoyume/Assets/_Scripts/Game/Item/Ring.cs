using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
