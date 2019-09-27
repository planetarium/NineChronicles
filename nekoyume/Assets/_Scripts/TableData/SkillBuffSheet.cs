using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class SkillBuffSheet : Sheet<int, SkillBuffSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => SkillId;
            public int SkillId { get; private set; }
            public List<int> BuffIds { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                SkillId = int.Parse(fields[0]);
                BuffIds = new List<int> {int.Parse(fields[1])};
            }

            public override void EndOfSheetInitialize()
            {
                BuffIds.Sort((a, b) =>
                {
                    if (a > b) return 1;
                    if (a < b) return -1;
                    return 0;
                });
            }
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
