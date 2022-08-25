using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldBossActionPatternSheet : Sheet<int, WorldBossActionPatternSheet.Row>
    {
        [Serializable]
        public class ActionPatternData
        {
            public int Wave;
            public readonly List<int> SkillIds = new List<int>();
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => BossId;
            public int BossId;
            public List<ActionPatternData> Patterns;

            public override void Set(IReadOnlyList<string> fields)
            {
                BossId = ParseInt(fields[0]);
                var patternData = new ActionPatternData()
                {
                    Wave = ParseInt(fields[1]),
                };

                for (int i = 2; i < fields.Count; ++i)
                {
                    var field = fields[i].Trim('\"');
                    var skillId = ParseInt(field);
                    patternData.SkillIds.Add(skillId);
                }

                Patterns = new List<ActionPatternData>() { patternData };
            }

            public override void EndOfSheetInitialize()
            {
                Patterns.Sort((left, right) =>
                {
                    if (left.Wave > right.Wave) return 1;
                    if (left.Wave < right.Wave) return -1;
                    return 0;
                });
            }
        }

        public WorldBossActionPatternSheet() : base(nameof(WorldBossActionPatternSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);
                return;
            }

            if (value.Patterns.Count == 0)
                return;

            row.Patterns.Add(value.Patterns[0]);
        }
    }
}
