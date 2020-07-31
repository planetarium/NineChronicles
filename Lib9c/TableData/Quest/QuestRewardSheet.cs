using System;
using System.Collections.Generic;
using System.Globalization;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class QuestRewardSheet : Sheet<int, QuestRewardSheet.Row>
    {
        [Serializable]
        public class Row: SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public List<int> RewardIds { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RewardIds = new List<int> {int.Parse(fields[1], CultureInfo.InvariantCulture)};
            }
        }

        public QuestRewardSheet() : base(nameof(QuestRewardSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.RewardIds.Count == 0)
                return;

            row.RewardIds.Add(value.RewardIds[0]);
        }
    }
}
