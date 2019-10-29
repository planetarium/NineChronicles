using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class QuestRewardSheet : Sheet<int, QuestRewardSheet.Row>
    {
        public class Row: SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public List<int> RewardIds { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                RewardIds = new List<int> {int.Parse(fields[1])};
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
