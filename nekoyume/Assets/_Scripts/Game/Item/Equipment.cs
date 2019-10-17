using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;

        public new EquipmentItemSheet.Row Data { get; }

        public int level;
        public int levelStats = 5;

        public Equipment(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
            Data = data;
        }

        public bool Equip()
        {

            equipped = true;
            return true;
        }

        public bool Unequip()
        {
            equipped = false;
            return true;
        }

        public void LevelUp()
        {
            level++;
            foreach (var statData in Data.Stats)
            {
                StatsMap.AddStatAdditionalValue(statData.StatType, level * levelStats);
            }
        }

    }
}
