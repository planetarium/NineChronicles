using System;
using System.Collections.Generic;
using Nekoyume.Model.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class GoldQuestSheet : Sheet<int, GoldQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public TradeType Type { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Type = (TradeType) Enum.Parse(typeof(TradeType), fields[3]);
            }
        }

        public GoldQuestSheet() : base(nameof(GoldQuestSheet))
        {
        }
    }
}
