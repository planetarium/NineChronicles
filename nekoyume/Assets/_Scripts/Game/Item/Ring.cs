using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(Data.Table.Item data, SkillBase skillBase = null) : base(data, skillBase)
        {
        }

    }
}
