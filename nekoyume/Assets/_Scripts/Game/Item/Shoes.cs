using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Shoes : Equipment
    {
        public Shoes(Data.Table.Item data, SkillBase skillBase = null) : base(data, skillBase)
        {
        }
    }
}
