using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Helm : Equipment
    {
        public Helm(Data.Table.Item data, SkillBase skillBase = null) : base(data, skillBase)
        {
        }

    }
}
