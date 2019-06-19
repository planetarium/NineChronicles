using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(Data.Table.Item data, SkillBase skillBase = null)
            : base(data, skillBase)
        {
        }
    }
}
