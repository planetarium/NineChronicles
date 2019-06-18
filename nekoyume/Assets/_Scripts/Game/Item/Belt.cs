using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        public Belt(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data, skillChance, skillEffect, skillElementalType)
        {
        }
    }
}
