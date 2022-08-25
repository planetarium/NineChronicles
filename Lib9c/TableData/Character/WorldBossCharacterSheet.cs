using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldBossCharacterSheet : Sheet<int, WorldBossCharacterSheet.Row>
    {
        [Serializable]
        public class WaveStatData
        {
            public int Wave { get; set; }
            public int TurnLimit { get; set; }
            public int EnrageTurn { get; set; }
            public int EnrageSkillId { get; set; }
            public ElementalType ElementalType { get; set; }
            public int Level { get; set; }
            public decimal HP { get; set; }
            public decimal ATK { get; set; }
            public decimal DEF { get; set; }
            public decimal CRI { get; set; }
            public decimal HIT { get; set; }
            public decimal SPD { get; set; }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => BossId;
            public int BossId { get; private set; }
            public List<WaveStatData> WaveStats { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                BossId = int.Parse(fields[0], CultureInfo.InvariantCulture);
                var statData = new WaveStatData()
                {
                    Wave = int.Parse(fields[1], CultureInfo.InvariantCulture),
                    TurnLimit = int.Parse(fields[2], CultureInfo.InvariantCulture),
                    EnrageTurn = int.Parse(fields[3], CultureInfo.InvariantCulture),
                    EnrageSkillId = int.Parse(fields[4], CultureInfo.InvariantCulture),
                    ElementalType = Enum.TryParse<ElementalType>(fields[5], out var elementalType)
                        ? elementalType
                        : ElementalType.Normal,
                    Level = int.Parse(fields[6], CultureInfo.InvariantCulture),
                    HP = TryParseDecimal(fields[7], out var hp) ? hp : 0m,
                    ATK = TryParseDecimal(fields[8], out var damage) ? damage : 0m,
                    DEF = TryParseDecimal(fields[9], out var defense) ? defense : 0m,
                    CRI = TryParseDecimal(fields[10], out var cri) ? cri : 0m,
                    HIT = TryParseDecimal(fields[11], out var hit) ? hit : 0m,
                    SPD = TryParseDecimal(fields[12], out var spd) ? spd : 0m,
                };
                WaveStats = new List<WaveStatData> { statData };
            }

            public override void EndOfSheetInitialize()
            {
                WaveStats.Sort((left, right) =>
                {
                    if (left.Wave > right.Wave) return 1;
                    if (left.Wave < right.Wave) return -1;
                    return 0;
                });
            }
        }
        
        public WorldBossCharacterSheet() : base(nameof(WorldBossCharacterSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);
                return;
            }

            if (value.WaveStats.Count == 0)
                return;

            row.WaveStats.Add(value.WaveStats[0]);
        }
    }

    public static class WorldBossStatSheetExtension
    {
        public static StatsMap ToStats(this WorldBossCharacterSheet.WaveStatData statData)
        {
            var hp = statData.HP;
            var atk = statData.ATK;
            var def = statData.DEF;
            var cri = statData.CRI;
            var hit = statData.HIT;
            var spd = statData.SPD;

            var stats = new StatsMap();
            stats.AddStatValue(StatType.HP, hp);
            stats.AddStatValue(StatType.ATK, atk);
            stats.AddStatValue(StatType.DEF, def);
            stats.AddStatValue(StatType.CRI, cri);
            stats.AddStatValue(StatType.HIT, hit);
            stats.AddStatValue(StatType.SPD, spd);

            return stats;
        }
    }
}
