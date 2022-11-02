using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RuneStatSheet : Sheet<int, RuneStatSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public class RuneStatInfo
            {
                public int Cp { get; }
                public List<(StatMap statMap, StatModifier.OperationType operationType)> Stats { get; set; }
                public RuneStatInfo(
                    List<(StatMap, StatModifier.OperationType)> stats, int cp)
                {
                    Stats = stats;
                    Cp = cp;
                }
            }

            public override int Key => RuneId;
            public int RuneId { get; private set; }
            public Dictionary<int, RuneStatInfo> LevelStatsMap { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                LevelStatsMap = new Dictionary<int, RuneStatInfo>();
                var stats = new List<(StatMap, StatModifier.OperationType)>();

                RuneId = ParseInt(fields[0]);
                var level = ParseInt(fields[1]);
                var cp = ParseInt(fields[2]);

                var statType1 = (StatType)Enum.Parse(typeof(StatType), fields[3]);
                var value1 = ParseDecimal(fields[4]);
                var valueType1 = (StatModifier.OperationType)
                    Enum.Parse(typeof(StatModifier.OperationType), fields[5]);
                if (statType1 != StatType.NONE)
                {
                    var statMap = new StatMap(statType1, value1);
                    stats.Add((statMap, valueType1));
                }

                var statType2 = (StatType)Enum.Parse(typeof(StatType), fields[6]);
                var value2 = ParseDecimal(fields[7]);
                var valueType2 = (StatModifier.OperationType)
                    Enum.Parse(typeof(StatModifier.OperationType), fields[8]);
                if (statType2 != StatType.NONE)
                {
                    var statMap = new StatMap(statType2, value2);
                    stats.Add((statMap, valueType2));
                }

                var statType3 = (StatType)Enum.Parse(typeof(StatType), fields[9]);
                var value3 = ParseDecimal(fields[10]);
                var valueType3 = (StatModifier.OperationType)
                    Enum.Parse(typeof(StatModifier.OperationType), fields[11]);
                if (statType3 != StatType.NONE)
                {
                    var statMap = new StatMap(statType3, value3);
                    stats.Add((statMap, valueType3));
                }

                LevelStatsMap[level] = new RuneStatInfo(stats, cp);
            }
        }

        public RuneStatSheet() : base(nameof(RuneStatSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.LevelStatsMap.Count == 0)
                return;

            var pair = value.LevelStatsMap.OrderBy(x => x.Key).First();
            row.LevelStatsMap[pair.Key] = pair.Value;
        }
    }
}
