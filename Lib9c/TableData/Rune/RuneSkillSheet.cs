using Nekoyume.Model.EnumType;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RuneSkillSheet : Sheet<int, RuneSkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public class RuneSkillInfo
            {
                public int SkillId { get; set; }
                public int Cooldown { get; set; }
                public int Chance { get; set; }
                public int Value { get; set; }
                public decimal StatRatio { get; set; }
                public StatType StatType { get; set; }
                public StatReferenceType StatReferenceType { get; set; }

                public RuneSkillInfo(
                    int skillId,
                    int cooldown,
                    int chance,
                    int value,
                    decimal statRatio,
                    StatType statType,
                    StatReferenceType statReferenceType)
                {
                    SkillId = skillId;
                    Cooldown = cooldown;
                    Chance = chance;
                    Value = value;
                    StatRatio = statRatio;
                    StatType = statType;
                    StatReferenceType = statReferenceType;
                }
            }

            public override int Key => RuneId;
            public int RuneId { get; private set; }
            public Dictionary<int, RuneSkillInfo> LevelSkillMap { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                LevelSkillMap = new Dictionary<int, RuneSkillInfo>();
                RuneId = ParseInt(fields[0]);

                var level = ParseInt(fields[1]);
                var skillId = ParseInt(fields[2]);
                var cooldown = ParseInt(fields[3]);
                var chance = ParseInt(fields[4]);
                var value = ParseInt(fields[5]);
                var statRatio = ParseDecimal(fields[6]);
                var statType =
                    (StatType)Enum.Parse(typeof(StatType), fields[7]);
                var statReferenceType =
                    (StatReferenceType)Enum.Parse(typeof(StatReferenceType), fields[8]);

                var skillInfo = new RuneSkillInfo(
                    skillId,
                    cooldown,
                    chance,
                    value,
                    statRatio,
                    statType,
                    statReferenceType);
                LevelSkillMap[level] = skillInfo;
            }
        }
        
        public RuneSkillSheet() : base(nameof(RuneSkillSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.LevelSkillMap.Count == 0)
                return;

            var pair = value.LevelSkillMap.OrderBy(x => x.Key).First();
            row.LevelSkillMap[pair.Key] = pair.Value;
        }
    }
}
