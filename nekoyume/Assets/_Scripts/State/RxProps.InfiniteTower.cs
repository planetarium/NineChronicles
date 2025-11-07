using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.TableData;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        private static readonly AsyncUpdatableRxProp<InfiniteTowerInfo> InfiniteTowerInfoInternal = new(UpdateInfiniteTowerInfoAsync);
        private static readonly ReactiveProperty<TicketProgress> InfiniteTowerTicketProgressInternal = new(new TicketProgress());
        private static readonly ReactiveProperty<InfiniteTowerScheduleSheet.Row> InfiniteTowerScheduleRowInternal = new(null);

        public static IReadOnlyAsyncUpdatableRxProp<InfiniteTowerInfo> InfiniteTowerInfo => InfiniteTowerInfoInternal;
        public static IReadOnlyReactiveProperty<TicketProgress> InfiniteTowerTicketProgress => InfiniteTowerTicketProgressInternal;
        public static IReadOnlyReactiveProperty<InfiniteTowerScheduleSheet.Row> InfiniteTowerScheduleRow => InfiniteTowerScheduleRowInternal;

        private static long _infiniteTowerInfoUpdatedBlockIndex;
        private static int _currentInfiniteTowerId = -1;

        private static void StartInfiniteTower()
        {
            OnBlockIndexInfiniteTower(_agent.BlockIndex);
            OnAvatarChangedInfiniteTower();

            InfiniteTowerScheduleRowInternal
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateInfiniteTowerTicketProgress(_agent.BlockIndex);
                })
                .AddTo(_disposables);
            InfiniteTowerInfoInternal
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    UpdateInfiniteTowerTicketProgress(_agent.BlockIndex);
                })
                .AddTo(_disposables);
        }

        private static void OnBlockIndexInfiniteTower(long blockIndex)
        {
            UpdateInfiniteTowerScheduleRow(blockIndex);
            UpdateInfiniteTowerTicketProgress(blockIndex);
        }

        private static void OnAvatarChangedInfiniteTower()
        {
            if (_currentInfiniteTowerId >= 0)
            {
                InfiniteTowerInfoInternal.UpdateAsync(_agent.BlockTipStateRootHash).Forget();
            }
        }

        private static void UpdateInfiniteTowerScheduleRow(long blockIndex)
        {
            var scheduleSheet = _tableSheets.InfiniteTowerScheduleSheet;
            if (scheduleSheet == null)
            {
                InfiniteTowerScheduleRowInternal.Value = null;
                InfiniteTowerInfoInternal.Value = null;
                _currentInfiniteTowerId = -1;
                return;
            }

            var activeSchedule = scheduleSheet.Values
                .FirstOrDefault(s => s.IsActive(blockIndex) || s.HasStarted(blockIndex));

            if (activeSchedule == null)
            {
                InfiniteTowerScheduleRowInternal.Value = null;
                InfiniteTowerInfoInternal.Value = null;
                _currentInfiniteTowerId = -1;
                return;
            }

            if (InfiniteTowerScheduleRowInternal.Value?.Id == activeSchedule.Id &&
                _currentInfiniteTowerId == activeSchedule.InfiniteTowerId)
            {
                return;
            }

            InfiniteTowerScheduleRowInternal.Value = activeSchedule;
            _currentInfiniteTowerId = activeSchedule.InfiniteTowerId;

            // Update InfiniteTowerInfo when schedule changes
            InfiniteTowerInfoInternal.UpdateAsync(_agent.BlockTipStateRootHash).Forget();
        }

        private static void UpdateInfiniteTowerTicketProgress(long blockIndex)
        {
            if (InfiniteTowerScheduleRowInternal.Value is null)
            {
                InfiniteTowerTicketProgressInternal.Value.Reset();
                InfiniteTowerTicketProgressInternal.SetValueAndForceNotify(
                    InfiniteTowerTicketProgressInternal.Value);
                return;
            }

            var scheduleRow = InfiniteTowerScheduleRowInternal.Value;
            var infiniteTowerInfo = InfiniteTowerInfoInternal.Value;

            // If InfiniteTowerInfo is not loaded yet, use default values
            if (infiniteTowerInfo == null || infiniteTowerInfo.InfiniteTowerId != scheduleRow.InfiniteTowerId)
            {
                InfiniteTowerTicketProgressInternal.Value.Reset(
                    0,
                    scheduleRow.MaxTickets,
                    0,
                    scheduleRow.ResetIntervalBlocks);
                InfiniteTowerTicketProgressInternal.SetValueAndForceNotify(
                    InfiniteTowerTicketProgressInternal.Value);
                return;
            }

            // Calculate remaining tickets considering reset and refill
            // Similar to EventDungeon's GetRemainingTicketsConsiderReset logic
            var resetIntervalBlockRange = scheduleRow.ResetIntervalBlocks;
            int currentTickets;

            if (infiniteTowerInfo.LastTicketRefillBlockIndex == 0)
            {
                // If LastTicketRefillBlockIndex is 0, check if reset should have occurred
                // based on schedule start
                var blockRange = blockIndex - scheduleRow.StartBlockIndex;
                if (blockRange <= 0)
                {
                    currentTickets = 0;
                }
                else
                {
                    var interval = (int)(blockRange / resetIntervalBlockRange);
                    // If interval > 0, reset should have occurred
                    currentTickets = interval > 0 ? scheduleRow.MaxTickets : infiniteTowerInfo.RemainingTickets;
                }
            }
            else
            {
                // Calculate if reset has occurred since last refill
                var blockRange = blockIndex - infiniteTowerInfo.LastTicketRefillBlockIndex;
                if (blockRange <= 0)
                {
                    currentTickets = infiniteTowerInfo.RemainingTickets;
                }
                else
                {
                    var interval = (int)(blockRange / resetIntervalBlockRange);
                    // If interval >= 1, reset has occurred
                    currentTickets = interval >= 1 ? scheduleRow.MaxTickets : infiniteTowerInfo.RemainingTickets;
                }
            }

            // Calculate progressed block range since last reset
            var progressedBlockRange = infiniteTowerInfo.LastTicketRefillBlockIndex > 0
                ? (blockIndex - infiniteTowerInfo.LastTicketRefillBlockIndex) % resetIntervalBlockRange
                : (blockIndex - scheduleRow.StartBlockIndex) % resetIntervalBlockRange;

            InfiniteTowerTicketProgressInternal.Value.Reset(
                currentTickets,
                scheduleRow.MaxTickets,
                (int)progressedBlockRange,
                resetIntervalBlockRange);
            InfiniteTowerTicketProgressInternal.SetValueAndForceNotify(
                InfiniteTowerTicketProgressInternal.Value);
        }

        private static async Task<InfiniteTowerInfo>
            UpdateInfiniteTowerInfoAsync(InfiniteTowerInfo previous, HashDigest<SHA256> stateRootHash)
        {
            if (_infiniteTowerInfoUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            if (!_currentAvatarAddr.HasValue ||
                InfiniteTowerScheduleRowInternal.Value is null)
            {
                return null;
            }

            var scheduleRow = InfiniteTowerScheduleRowInternal.Value;
            // InfiniteTowerInfo는 Addresses.InfiniteTowerInfo 계정에 avatarAddress를 key로 저장됨
            // GetStateAsync(accountAddress, keyAddress) 형식으로 호출
            var state = await _agent.GetStateAsync(stateRootHash, Addresses.InfiniteTowerInfo, _currentAvatarAddr.Value);
            if (state is Bencodex.Types.List serialized)
            {
                var infiniteTowerInfo = new InfiniteTowerInfo(serialized);
                _infiniteTowerInfoUpdatedBlockIndex = _agent.BlockIndex;
                return infiniteTowerInfo;
            }

            // If no state exists, create a new InfiniteTowerInfo
            return new InfiniteTowerInfo(_currentAvatarAddr.Value, scheduleRow.InfiniteTowerId);
        }
    }
}
