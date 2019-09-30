using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Model;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SetEffectSheet : Sheet<int, SetEffectSheet.Row>
    {
        [Serializable]
        public class StatDataWithCount : StatData
        {
            public int Count { get; }

            public StatDataWithCount(StatType statType, decimal value, int count) : base(statType, value)
            {
                Count = count;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<StatDataWithCount> Stats { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Stats = new List<StatDataWithCount>
                {
                    new StatDataWithCount(
                        (StatType) Enum.Parse(typeof(StatType), fields[2]),
                        decimal.Parse(fields[3]),
                        int.Parse(fields[1])
                    )
                };
            }
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            foreach (var stat in value.Stats)
            {
                row.Stats.Add(stat);

                break;
            }
        }

        public IEnumerable<StatData> GetSetEffect(int id, int count)
        {
            var statMaps = new List<StatData>();
            if (!TryGetValue(id, out var row))
                return statMaps;

            foreach (var stat in row.Stats)
            {
                if (stat.Count > count)
                    break;

                statMaps.Add(stat);
            }

            return statMaps;
        }
    }
}
