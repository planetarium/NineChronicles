using System;

namespace Nekoyume.TableData
{
    [Serializable]
    public class BattleQuestSheet : Sheet<int, BattleQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
        }
        
        public BattleQuestSheet() : base(nameof(BattleQuestSheet))
        {
        }
    }
}
