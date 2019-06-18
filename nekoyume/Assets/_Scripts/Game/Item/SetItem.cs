using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class SetItem : Equipment
    {
        public SetItem(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data, skillChance, skillEffect, skillElementalType)
        {
        }

        public static Dictionary<int, int> WeaponMap =>
            new Dictionary<int, int>
            {
                [308001] = 301001,
                [308002] = 301002,
                [308003] = 301003,
            };
    }
}
