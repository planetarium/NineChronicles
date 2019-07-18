using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Helm : Equipment
    {
        public Helm(Data.Table.Item data, SkillBase skillBase = null, string id = null)
            : base(data, skillBase, id)
        {
        }
    }
}
