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

        // private static readonly ReactiveProperty<int>
        //     _remainingEventTicketsConsiderReset = new(0);
        //
        // public static IReadOnlyReactiveProperty<int>
        //     RemainingEventTicketsConsiderReset => _remainingEventTicketsConsiderReset;
        
        private static readonly ReactiveProperty<TicketProgress>
            _eventDungeonTicketProgress = new(new TicketProgress());

        public static IReadOnlyReactiveProperty<TicketProgress>
            EventDungeonTicketProgress => _eventDungeonTicketProgress;

        private static void StartEvent()
        {
            OnBlockIndexEvent(_agent.BlockIndex);
            OnAvatarChangedEvent();

            EventDungeonInfo
                .Subscribe(_ => UpdateEventDungeonTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);
        }

        private static void OnBlockIndexEvent(long blockIndex)
        {
            UpdateEventDungeonSheetData(blockIndex);
            UpdateEventDungeonTicketProgress(blockIndex);
        }

        private static void OnAvatarChangedEvent()
        {
            _eventDungeonInfo.UpdateAsync().Forget();
        }

        private static void UpdateEventDungeonSheetData(long blockIndex)
        {
            if (!_tableSheets.EventScheduleSheet.TryGetRowForDungeon(
                    blockIndex,
                    out var scheduleRow))
            {
                EventScheduleRowForDungeon = null;
                EventDungeonRow = null;
                EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
                EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
                return;
            }

            if (EventScheduleRowForDungeon is not null &&
                EventScheduleRowForDungeon.Id == scheduleRow.Id)
            {
                return;
            }

            EventScheduleRowForDungeon = scheduleRow;
            if (!_tableSheets.EventDungeonSheet.TryGetRowByEventScheduleId(
                    EventScheduleRowForDungeon.Id,
                    out var dungeonRow))
            {
                EventDungeonRow = null;
                EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
                EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
                return;
            }

            if (EventDungeonRow is not null &&
                EventDungeonRow.Id == dungeonRow.Id)
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
        }

        // private static void UpdateRemainingEventTicketsConsiderReset(long blockIndex)
        // {
        //     _remainingEventTicketsConsiderReset.Value =
        //         EventDungeonInfo.Value.GetRemainingTicketsConsiderReset(
        //             EventScheduleRowForDungeon,
        //             blockIndex);
        // }
        
        private static void UpdateEventDungeonTicketProgress(long blockIndex)
        {
            if (EventScheduleRowForDungeon is null)
            {
                _eventDungeonTicketProgress.Value.Reset();
                _eventDungeonTicketProgress.SetValueAndForceNotify(
                    _eventDungeonTicketProgress.Value);
                return;
            }

            var current = EventDungeonInfo.Value
                .GetRemainingTicketsConsiderReset(
                    EventScheduleRowForDungeon,
                    blockIndex);
            var resetIntervalBlockRange =
                EventScheduleRowForDungeon.DungeonTicketsResetIntervalBlockRange;
            var progressedBlockRange =
                (blockIndex - EventScheduleRowForDungeon.StartBlockIndex)
                % resetIntervalBlockRange;

            _eventDungeonTicketProgress.Value.Reset(
                current,
                EventScheduleRowForDungeon.DungeonTicketsMax,
                (int)progressedBlockRange,
                resetIntervalBlockRange);
            _eventDungeonTicketProgress.SetValueAndForceNotify(
                _eventDungeonTicketProgress.Value);
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
