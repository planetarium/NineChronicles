using System;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldQuestSheet : Sheet<int, WorldQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
        }
        
        public WorldQuestSheet() : base(nameof(WorldQuestSheet))
        {
        }
    }
}
