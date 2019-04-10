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
            var equipData = (ItemEquipment) Data;
            var stat1 = new StatsMap
            {
                Key = equipData.ability1,
                Value = equipData.value1,
            };
            var stat2 = new StatsMap
            {
                Key = equipData.ability2,
                Value = equipData.value2,
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
