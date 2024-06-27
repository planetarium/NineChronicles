using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Model.Event;
using Nekoyume.TableData.Event;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        // Dungeon
        private static readonly ReactiveProperty<EventScheduleSheet.Row>
            _eventScheduleRowForDungeon = new(null);

        public static IReadOnlyReactiveProperty<EventScheduleSheet.Row>
            EventScheduleRowForDungeon => _eventScheduleRowForDungeon;

        public static EventDungeonSheet.Row EventDungeonRow { get; private set; }

        public static List<EventDungeonStageSheet.Row>
            EventDungeonStageRows { get; private set; }

        public static List<EventDungeonStageWaveSheet.Row>
            EventDungeonStageWaveRows { get; private set; }

        private static readonly AsyncUpdatableRxProp<EventDungeonInfo>
            _eventDungeonInfo = new(UpdateEventDungeonInfoAsync);

        private static long _eventDungeonInfoUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<EventDungeonInfo>
            EventDungeonInfo => _eventDungeonInfo;

        private static readonly ReactiveProperty<TicketProgress>
            _eventDungeonTicketProgress = new(new TicketProgress());

        public static IReadOnlyReactiveProperty<TicketProgress>
            EventDungeonTicketProgress => _eventDungeonTicketProgress;

        private static ReactiveProperty<string> _eventDungeonRemainingTimeText =
            new(string.Empty);

        public static IReadOnlyReactiveProperty<string> EventDungeonRemainingTimeText =>
            _eventDungeonRemainingTimeText;

        // Recipe
        private static readonly ReactiveProperty<EventScheduleSheet.Row>
            _eventScheduleRowForRecipe = new(null);

        public static IReadOnlyReactiveProperty<EventScheduleSheet.Row>
            EventScheduleRowForRecipe => _eventScheduleRowForRecipe;

        private static readonly ReactiveProperty<List<EventConsumableItemRecipeSheet.Row>>
            _eventConsumableItemRecipeRows = new(null);

        public static IReadOnlyReactiveProperty<List<EventConsumableItemRecipeSheet.Row>>
            EventConsumableItemRecipeRows => _eventConsumableItemRecipeRows;

        private static readonly ReactiveProperty<List<EventMaterialItemRecipeSheet.Row>>
            _eventMaterialItemRecipeRows = new(null);

        public static IReadOnlyReactiveProperty<List<EventMaterialItemRecipeSheet.Row>>
            EventMaterialItemRecipeRows = _eventMaterialItemRecipeRows;

        private static ReactiveProperty<string> _eventRecipeRemainingTimeText =
            new(string.Empty);

        public static IReadOnlyReactiveProperty<string> EventRecipeRemainingTimeText =>
            _eventRecipeRemainingTimeText;

        private static void StartEvent()
        {
            OnBlockIndexEvent(_agent.BlockIndex);
            OnAvatarChangedEvent();

            _eventScheduleRowForDungeon
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateEventDungeonTicketProgress(_agent.BlockIndex);
                    UpdateEventDungeonRemainingTimeText(_agent.BlockIndex);
                })
                .AddTo(_disposables);
            _eventDungeonInfo
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateEventDungeonTicketProgress(_agent.BlockIndex);
                    UpdateEventDungeonRemainingTimeText(_agent.BlockIndex);
                })
                .AddTo(_disposables);
            _eventScheduleRowForRecipe
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
            _eventDungeonInfo.UpdateAsync(_agent.BlockTipStateRootHash).Forget();
        }

        private static void UpdateEventDungeonSheetData(long blockIndex)
        {
            if (!_tableSheets.EventScheduleSheet.TryGetRowForDungeon(
                    blockIndex,
                    out var scheduleRow) ||
                scheduleRow.DungeonEndBlockIndex == blockIndex)
            {
                _eventScheduleRowForDungeon.Value = null;
                EventDungeonRow = null;
                EventDungeonStageRows = new List<EventDungeonStageSheet.Row>();
                EventDungeonStageWaveRows = new List<EventDungeonStageWaveSheet.Row>();
                _eventDungeonInfo.Value = null;
                return;
            }

            if (_eventScheduleRowForDungeon.Value?.Id == scheduleRow.Id)
            {
                return;
            }

            _eventScheduleRowForDungeon.Value = scheduleRow;
            if (!_tableSheets.EventDungeonSheet.TryGetRowByEventScheduleId(
                    _eventScheduleRowForDungeon.Value.Id,
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
            if (_eventScheduleRowForDungeon.Value is null)
            {
                _eventDungeonTicketProgress.Value.Reset();
                _eventDungeonTicketProgress.SetValueAndForceNotify(
                    _eventDungeonTicketProgress.Value);
                return;
            }

            var current = _eventDungeonInfo.Value
                .GetRemainingTicketsConsiderReset(
                    _eventScheduleRowForDungeon.Value,
                    blockIndex);
            var resetIntervalBlockRange =
                _eventScheduleRowForDungeon.Value.DungeonTicketsResetIntervalBlockRange;
            var progressedBlockRange =
                (blockIndex - _eventScheduleRowForDungeon.Value.StartBlockIndex)
                % resetIntervalBlockRange;

            _eventDungeonTicketProgress.Value.Reset(
                current,
                _eventScheduleRowForDungeon.Value.DungeonTicketsMax,
                (int)progressedBlockRange,
                resetIntervalBlockRange);
            _eventDungeonTicketProgress.SetValueAndForceNotify(
                _eventDungeonTicketProgress.Value);
        }

        private static void UpdateEventDungeonRemainingTimeText(long blockIndex)
        {
            if (_eventScheduleRowForDungeon.Value is null)
            {
                _eventDungeonRemainingTimeText.SetValueAndForceNotify(string.Empty);
                return;
            }

            var value = _eventScheduleRowForDungeon.Value.DungeonEndBlockIndex - blockIndex;
            var time = value.BlockRangeToTimeSpanString();
            _eventDungeonRemainingTimeText.SetValueAndForceNotify($"{value}({time})");
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
                _eventScheduleRowForRecipe.Value = null;
                _eventConsumableItemRecipeRows.Value = null;
                _eventMaterialItemRecipeRows.Value = null;
                return;
            }

            if (_eventScheduleRowForRecipe.Value?.Id == scheduleRow.Id)
            {
                return;
            }

            _eventScheduleRowForRecipe.Value = scheduleRow;
            _eventConsumableItemRecipeRows.Value =
                _tableSheets.EventConsumableItemRecipeSheet
                    .GetRecipeRows(_eventScheduleRowForRecipe.Value.Id);
            _eventMaterialItemRecipeRows.Value =
                _tableSheets.EventMaterialItemRecipeSheet
                    .GetRecipeRows(_eventScheduleRowForRecipe.Value.Id);
        }

        private static void UpdateEventRecipeRemainingTimeText(long blockIndex)
        {
            if (_eventScheduleRowForRecipe.Value is null)
            {
                _eventRecipeRemainingTimeText.SetValueAndForceNotify(string.Empty);
                return;
            }

            var value = _eventScheduleRowForRecipe.Value.RecipeEndBlockIndex - blockIndex;
            var time = value.BlockRangeToTimeSpanString();
            _eventRecipeRemainingTimeText.SetValueAndForceNotify($"{value}({time})");
        }
    }
}
