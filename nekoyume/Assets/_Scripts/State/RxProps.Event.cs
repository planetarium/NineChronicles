using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.Extensions;
using Nekoyume.Model.Event;
using Nekoyume.TableData.Event;
using UniRx;

namespace Nekoyume.State
{
    public static partial class RxProps
    {
        public static EventScheduleSheet.Row EventScheduleRowForDungeon { get; private set; }

        public static EventDungeonSheet.Row EventDungeonRow { get; private set; }

        public static List<EventDungeonStageSheet.Row> EventDungeonStageRows { get; private set; }

        public static List<EventDungeonStageWaveSheet.Row> EventDungeonStageWaveRows { get; private set; }

        private static readonly AsyncUpdatableRxProp<EventDungeonInfo>
            _eventDungeonInfo = new(UpdateEventDungeonInfoAsync);

        private static long _eventDungeonInfoUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<EventDungeonInfo>
            EventDungeonInfo => _eventDungeonInfo;

        private static readonly ReactiveProperty<int>
            _remainingEventTicketsConsiderReset = new(0);

        public static IReadOnlyReactiveProperty<int>
            RemainingEventTicketsConsiderReset => _remainingEventTicketsConsiderReset;

        private static void StartEvent()
        {
            EventScheduleRowForDungeon = null;
            EventDungeonRow = null;
            EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
            EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
            var blockIndex = _agent.BlockIndex;
            if (!_tableSheets.EventScheduleSheet.TryGetRowForDungeon(
                    blockIndex,
                    out var scheduleRow))
            {
                return;
            }

            EventScheduleRowForDungeon = scheduleRow;
            if (!_tableSheets.EventDungeonSheet.TryGetRowByEventScheduleId(
                    EventScheduleRowForDungeon.Id,
                    out var dungeonRow))
            {
                return;
            }

            EventDungeonRow = dungeonRow;
            EventDungeonStageRows = _tableSheets.EventDungeonStageSheet.GetStageRows(
                EventDungeonRow.StageBegin,
                EventDungeonRow.StageEnd);
            EventDungeonStageWaveRows = _tableSheets.EventDungeonStageWaveSheet.GetStageWaveRows(
                EventDungeonRow.StageBegin,
                EventDungeonRow.StageEnd);

            OnBlockIndexEvent(_agent.BlockIndex);
            OnAvatarChangedEvent();

            EventDungeonInfo
                .Subscribe(_ => UpdateRemainingEventTicketsConsiderReset(_agent.BlockIndex))
                .AddTo(_disposables);
        }

        private static void OnBlockIndexEvent(long blockIndex)
        {
            UpdateRemainingEventTicketsConsiderReset(blockIndex);
        }

        private static void OnAvatarChangedEvent()
        {
            _eventDungeonInfo.UpdateAsync().Forget();
        }

        private static void UpdateRemainingEventTicketsConsiderReset(long blockIndex)
        {
            _remainingEventTicketsConsiderReset.Value =
                EventDungeonInfo.Value.GetRemainingTicketsConsiderReset(
                    EventScheduleRowForDungeon,
                    blockIndex);
        }

        private static async Task<EventDungeonInfo>
            UpdateEventDungeonInfoAsync(EventDungeonInfo previous)
        {
            if (_eventDungeonInfoUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            if (!_currentAvatarAddr.HasValue ||
                EventDungeonRow is null)
            {
                return null;
            }

            var addr = Nekoyume.Model.Event.EventDungeonInfo.DeriveAddress(
                _currentAvatarAddr.Value,
                EventDungeonRow.Id);
            return await _agent.GetStateAsync(addr)
                is Bencodex.Types.List serialized
                ? new EventDungeonInfo(serialized)
                : null;
        }
    }
}
