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
            var stat1 = new StatsMap
            {
                Key = Data.ability1,
                Value = Data.value1,
            };
            var stat2 = new StatsMap
            {
                Key = Data.ability2,
                Value = Data.value2,
            };
            Stats = new[] {stat1, stat2};
        }
    }
}
