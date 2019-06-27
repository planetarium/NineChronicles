using System;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Food : ItemUsable
    {
        public Food(Data.Table.Item data, SkillBase skillBase = null)
            : base(data, skillBase)
        {
            Stats.SetStatValue(Data.ability1, Data.value1);
            Stats.SetStatValue(Data.ability2, Data.value2);
        }
    }
}
