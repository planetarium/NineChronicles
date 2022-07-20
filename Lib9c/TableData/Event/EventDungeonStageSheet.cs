using System;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventDungeonStageSheet : Sheet<int, EventDungeonStageSheet.Row>
    {
        [Serializable]
        public class Row : StageSheet.Row
        {
        }

        public EventDungeonStageSheet() : base(nameof(EventDungeonStageSheet))
        {
        }
    }
}
