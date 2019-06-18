using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        private const int SynergyMultiplier = 2;
        public Belt(Data.Table.Item data, SkillBase skillBase = null) : base(data, skillBase)
        {
        }

    }
}
