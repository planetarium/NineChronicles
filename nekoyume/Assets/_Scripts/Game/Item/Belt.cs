using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        public Belt(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
