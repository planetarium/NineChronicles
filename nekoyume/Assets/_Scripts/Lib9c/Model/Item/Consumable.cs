using System;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Consumable : ItemUsable
    {
        public new ConsumableItemSheet.Row Data { get; }
        public StatType MainStat
        {
            get
            {
                if (Data.Stats.Count == 0)
                    return StatType.NONE;

                // 임시로 첫번재 스탯을 리턴
                return Data.Stats[0].StatType;
            }
        }

        public Consumable(ConsumableItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
            Data = data;
        }
    }
}
