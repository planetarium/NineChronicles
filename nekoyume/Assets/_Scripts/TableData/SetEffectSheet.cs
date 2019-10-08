using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SetEffectSheet : Sheet<int, SetEffectSheet.Row>
    {
        [Serializable]
        public class StatMapWithCount : StatMap
        {
            public int Count { get; }

            public StatMapWithCount(StatType statType, decimal value, int count) : base(statType, value)
            {
                Count = count;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<StatMapWithCount> Stats { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Stats = new List<StatMapWithCount>
                {
                    new StatMapWithCount(
                        (StatType) Enum.Parse(typeof(StatType), fields[2]),
                        decimal.Parse(fields[3]),
                        int.Parse(fields[1])
                    )
                };
            }
        }
        
        public SetEffectSheet() : base(nameof(SetEffectSheet))
        {
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

        public IEnumerable<StatMap> GetSetEffect(int id, int count)
        {
            var statMaps = new List<StatMap>();
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

    public static class SetEffectExtension
    {
        public static List<SetEffectSheet.Row> GetSetEffectRows(this SetEffectSheet sheet, IEnumerable<Equipment> equipments)
        {
            var setMap = new Dictionary<int, int>();
            foreach (var equipment in equipments)
            {
                var key = equipment.Data.SetId;
                if (!setMap.ContainsKey(key))
                {
                    setMap[key] = 0;
                }

                setMap[key] += 1;
            }

            var statMaps = new List<SetEffectSheet.Row>();
            foreach (var pair in setMap)
            {
                if (!sheet.TryGetValue(pair.Key, out var row))
                    continue;

                foreach (var stat in row.Stats)
                {
                    if (stat.Count > pair.Value)
                        break;

                    statMaps.Add(row);
                }
            }
            
            return statMaps;
        }
    }
}
