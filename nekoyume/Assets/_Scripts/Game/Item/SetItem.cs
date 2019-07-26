using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class SetItem : Equipment
    {
        public SetItem(Data.Table.Item data, Guid id, SkillBase skillBase = null)
            : base(data, id, skillBase)
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
