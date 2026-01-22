using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Model.Event;
using Nekoyume.TableData.Event;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        public static EventDungeonSheet.Row EventDungeonRow { get; private set; }
        public static List<EventDungeonStageSheet.Row> EventDungeonStageRows { get; private set; }
        public static List<EventDungeonStageWaveSheet.Row> EventDungeonStageWaveRows { get; private set; }

        private static readonly ReactiveProperty<EventScheduleSheet.Row> EventScheduleRowForDungeonInternal = new(null);
        private static readonly AsyncUpdatableRxProp<EventDungeonInfo> EventDungeonInfoInternal = new(UpdateEventDungeonInfoAsync);
        private static readonly ReactiveProperty<TicketProgress> EventDungeonTicketProgressInternal = new(new TicketProgress());
        private static readonly ReactiveProperty<string> EventRecipeRemainingTimeTextInternal = new(string.Empty);
        private static readonly ReactiveProperty<string> EventDungeonRemainingTimeTextInternal = new(string.Empty);
        private static readonly ReactiveProperty<EventScheduleSheet.Row> EventScheduleRowForRecipeInternal = new(null);
        private static readonly ReactiveProperty<List<EventConsumableItemRecipeSheet.Row>>
            EventConsumableItemRecipeRowsInternal = new(null);
        private static readonly ReactiveProperty<List<EventMaterialItemRecipeSheet.Row>>
            EventMaterialItemRecipeRowsInternal = new(null);

        public static IReadOnlyReactiveProperty<EventScheduleSheet.Row> EventScheduleRowForDungeon => EventScheduleRowForDungeonInternal;
        public static IReadOnlyAsyncUpdatableRxProp<EventDungeonInfo> EventDungeonInfo => EventDungeonInfoInternal;
        public static IReadOnlyReactiveProperty<TicketProgress> EventDungeonTicketProgress => EventDungeonTicketProgressInternal;
        public static IReadOnlyReactiveProperty<string> EventRecipeRemainingTimeText => EventRecipeRemainingTimeTextInternal;
        public static IReadOnlyReactiveProperty<string> EventDungeonRemainingTimeText => EventDungeonRemainingTimeTextInternal;
        public static IReadOnlyReactiveProperty<EventScheduleSheet.Row> EventScheduleRowForRecipe => EventScheduleRowForRecipeInternal;
        public static IReadOnlyReactiveProperty<List<EventConsumableItemRecipeSheet.Row>>
            EventConsumableItemRecipeRows => EventConsumableItemRecipeRowsInternal;
        public static IReadOnlyReactiveProperty<List<EventMaterialItemRecipeSheet.Row>>
            EventMaterialItemRecipeRows => EventMaterialItemRecipeRowsInternal;

        private static long _eventDungeonInfoUpdatedBlockIndex;

        private static void StartEvent()
        {
            OnBlockIndexEvent(_agent.BlockIndex);
            OnAvatarChangedEvent();

            EventScheduleRowForDungeonInternal
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateEventDungeonTicketProgress(_agent.BlockIndex);
                    UpdateEventDungeonRemainingTimeText(_agent.BlockIndex);
                })
                .AddTo(_disposables);
            EventDungeonInfoInternal
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateEventDungeonTicketProgress(_agent.BlockIndex);
                    UpdateEventDungeonRemainingTimeText(_agent.BlockIndex);
                })
                .AddTo(_disposables);
            EventScheduleRowForRecipeInternal
                .ObserveOnMainThread()
                .Subscribe(_ => UpdateEventRecipeRemainingTimeText(_agent.BlockIndex))
                .AddTo(_disposables);
        }

        private static void OnBlockIndexEvent(long blockIndex)
        {
            UpdateEventDungeonSheetData(blockIndex);
            UpdateEventDungeonTicketProgress(blockIndex);
            UpdateEventDungeonRemainingTimeText(blockIndex);
            UpdateEventRecipeSheetData(blockIndex);
            UpdateEventRecipeRemainingTimeText(blockIndex);
        }

        private static void OnAvatarChangedEvent()
        {
            EventDungeonInfoInternal.UpdateAsync(_agent.BlockTipStateRootHash).Forget();
        }

        private static void UpdateEventDungeonSheetData(long blockIndex)
        {
            if (!_tableSheets.EventScheduleSheet.TryGetRowForDungeon(
                    blockIndex,
                    out var scheduleRow) ||
                scheduleRow.DungeonEndBlockIndex == blockIndex)
            {
                EventScheduleRowForDungeonInternal.Value = null;
                EventDungeonRow = null;
                EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
                EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
                EventDungeonInfoInternal.Value = null;
                return;
            }

            if (EventScheduleRowForDungeonInternal.Value?.Id == scheduleRow.Id)
            {
                return;
            }

            EventScheduleRowForDungeonInternal.Value = scheduleRow;
            if (!_tableSheets.EventDungeonSheet.TryGetRowByEventScheduleId(
                EventScheduleRowForDungeonInternal.Value.Id,
                out var dungeonRow))
            {
                EventDungeonRow = null;
                EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
                EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
                return;
            }

            if (EventDungeonRow?.Id == dungeonRow.Id)
            {
                return;
            }

            EventDungeonRow = dungeonRow;
            EventDungeonStageRows = _tableSheets.EventDungeonStageSheet
                .GetStageRows(
                    EventDungeonRow.StageBegin,
                    EventDungeonRow.StageEnd);
            EventDungeonStageWaveRows = _tableSheets.EventDungeonStageWaveSheet
                .GetStageWaveRows(
                    EventDungeonRow.StageBegin,
                    EventDungeonRow.StageEnd);
        }

        private static void UpdateEventDungeonTicketProgress(long blockIndex)
        {
            if (EventScheduleRowForDungeonInternal.Value is null)
            {
                EventDungeonTicketProgressInternal.Value.Reset();
                EventDungeonTicketProgressInternal.SetValueAndForceNotify(
                    EventDungeonTicketProgressInternal.Value);
                return;
            }

            var current = EventDungeonInfoInternal.Value
                .GetRemainingTicketsConsiderReset(
                    EventScheduleRowForDungeonInternal.Value,
                    blockIndex);
            var resetIntervalBlockRange =
                EventScheduleRowForDungeonInternal.Value.DungeonTicketsResetIntervalBlockRange;
            var progressedBlockRange =
                (blockIndex - EventScheduleRowForDungeonInternal.Value.StartBlockIndex)
                % resetIntervalBlockRange;

            EventDungeonTicketProgressInternal.Value.Reset(
                current,
                EventScheduleRowForDungeonInternal.Value.DungeonTicketsMax,
                (int)progressedBlockRange,
                resetIntervalBlockRange);
            EventDungeonTicketProgressInternal.SetValueAndForceNotify(
                EventDungeonTicketProgressInternal.Value);
        }

        private static void UpdateEventDungeonRemainingTimeText(long blockIndex)
        {
            if (EventScheduleRowForDungeonInternal.Value is null)
            {
                EventDungeonRemainingTimeTextInternal.SetValueAndForceNotify(string.Empty);
                return;
            }

            var value = EventScheduleRowForDungeonInternal.Value.DungeonEndBlockIndex - blockIndex;
            var time = value.BlockRangeToTimeSpanString();
            EventDungeonRemainingTimeTextInternal.SetValueAndForceNotify($"{value}({time})");
        }

        private static async Task<EventDungeonInfo>
            UpdateEventDungeonInfoAsync(EventDungeonInfo previous, HashDigest<SHA256> stateRootHash)
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
            return await _agent.GetStateAsync(stateRootHash, ReservedAddresses.LegacyAccount, addr)
                is Bencodex.Types.List serialized
                ? new EventDungeonInfo(serialized)
                : null;
        }

        private static void UpdateEventRecipeSheetData(long blockIndex)
        {
            if (!_tableSheets.EventScheduleSheet.TryGetRowForRecipe(blockIndex, out var scheduleRow)
                || scheduleRow.RecipeEndBlockIndex == blockIndex)
            {
                EventScheduleRowForRecipeInternal.Value = null;
                EventConsumableItemRecipeRowsInternal.Value = null;
                EventMaterialItemRecipeRowsInternal.Value = null;
                return;
            }

            if (EventScheduleRowForRecipeInternal.Value?.Id == scheduleRow.Id)
            {
                return;
            }

            EventScheduleRowForRecipeInternal.Value = scheduleRow;
            EventConsumableItemRecipeRowsInternal.Value =
                _tableSheets.EventConsumableItemRecipeSheet
                    .GetRecipeRows(EventScheduleRowForRecipeInternal.Value.Id);
            EventMaterialItemRecipeRowsInternal.Value =
                _tableSheets.EventMaterialItemRecipeSheet
                    .GetRecipeRows(EventScheduleRowForRecipeInternal.Value.Id);
        }

        private static void UpdateEventRecipeRemainingTimeText(long blockIndex)
        {
            if (EventScheduleRowForRecipeInternal.Value is null)
            {
                EventRecipeRemainingTimeTextInternal.SetValueAndForceNotify(string.Empty);
                return;
            }

            var value = EventScheduleRowForRecipeInternal.Value.RecipeEndBlockIndex - blockIndex;
            var time = value.BlockRangeToTimeSpanString();
            EventRecipeRemainingTimeTextInternal.SetValueAndForceNotify($"{value}({time})");
        }
    }
}
