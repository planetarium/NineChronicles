using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Model;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ConsumableItemSheet : Sheet<int, ConsumableItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Consumable;

            public int SetId { get; private set; }
            public List<StatData> Stats { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                SetId = string.IsNullOrEmpty(fields[4]) ? 0 : int.Parse(fields[4]);
                Stats = new List<StatData>();

                for (var i = 0; i < 2; i++)
                {
                    if (string.IsNullOrEmpty(fields[5 + i * 2]) ||
                        string.IsNullOrEmpty(fields[6 + i * 2]))
                        return;

                    Stats.Add(new StatData(
                        (StatType) Enum.Parse(typeof(StatType), fields[5 + i * 2]),
                        decimal.Parse(fields[6 + i * 2])));
                }
            }
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.Stats.Count == 0)
                return;

            row.Stats.Add(value.Stats[0]);
        }
    }
}
