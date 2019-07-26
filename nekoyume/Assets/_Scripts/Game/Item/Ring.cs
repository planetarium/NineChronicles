using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(Data.Table.Item data, Guid id, SkillBase skillBase = null)
            : base(data, id, skillBase)
        {
        }
    }
}
