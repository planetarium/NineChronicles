using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class TradeQuestSheet : Sheet<int, TradeQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public string Type { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Type = fields[3];
            }
        }
    }
}
