using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Food : ItemUsable
    {
        private readonly StatsMap[] _stats;
        public Food(Data.Table.Item data) : base(data)
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
            _stats = new[] {stat1, stat2};
        }

        public void Use(Player player)
        {
            foreach (var stat in _stats)
            {
                stat.UpdatePlayer(player);
            }
        }
    }
}
