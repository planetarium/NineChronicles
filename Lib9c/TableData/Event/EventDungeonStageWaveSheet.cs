using System;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventDungeonStageWaveSheet : Sheet<int, EventDungeonStageWaveSheet.Row>
    {
        [Serializable]
        public class Row : StageWaveSheet.Row
        {
        }

        public EventDungeonStageWaveSheet() : base(nameof(EventDungeonStageWaveSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.Waves.Count == 0)
                return;

            row.Waves.Add(value.Waves[0]);
        }
    }
}
