using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class QuestSheet : Sheet<int, QuestSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int Goal { get; private set; }
            public decimal Reward { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Goal = int.Parse(fields[1]);
                Reward = decimal.Parse(fields[2]);
            }
        }
        
        public QuestSheet() : base(nameof(QuestSheet))
        {
        }
    }
}
