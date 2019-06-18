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
        public Food(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data, skillChance, skillEffect, skillElementalType)
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

        public override string ToItemInfo()
        {
            var infos = Stats
                .Select(stat => stat.GetInformation())
                .Where(info => !string.IsNullOrEmpty(info));
            return string.Join(Environment.NewLine, infos);
        }
    }
}
