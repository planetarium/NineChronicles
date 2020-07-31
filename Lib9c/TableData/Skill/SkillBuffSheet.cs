using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SkillBuffSheet : Sheet<int, SkillBuffSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => SkillId;
            public int SkillId { get; private set; }
            public List<int> BuffIds { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                SkillId = ParseInt(fields[0]);
                BuffIds = new List<int> {ParseInt(fields[1])};
            }

            public override void EndOfSheetInitialize()
            {
                BuffIds.Sort((left, right) =>
                {
                    if (left > right) return 1;
                    if (left < right) return -1;
                    return 0;
                });
            }
        }
        
        public SkillBuffSheet() : base(nameof(SkillBuffSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.BuffIds.Count == 0)
                return;

            row.BuffIds.Add(value.BuffIds[0]);
        }
    }
}
