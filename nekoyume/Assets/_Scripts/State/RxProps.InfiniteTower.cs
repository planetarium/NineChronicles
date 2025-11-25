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
                .FirstOrDefault(s => s.IsActive(blockIndex));

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

            // Calculate remaining tickets using the same logic as TryRefillDailyTickets
            // This ensures client UI matches server's actual ticket count
            var resetIntervalBlockRange = scheduleRow.ResetIntervalBlocks;
            int currentTickets;
            long lastRefillBlockIndex = infiniteTowerInfo.LastTicketRefillBlockIndex;

            // If LastTicketRefillBlockIndex is 0, initialize the reference point and do not refill immediately
            // (Same logic as TryRefillDailyTickets)
            if (lastRefillBlockIndex == 0)
            {
                // No refill has occurred yet, use current RemainingTickets (same as server)
                currentTickets = infiniteTowerInfo.RemainingTickets;
                // Use schedule start as reference for progressed block range calculation
                lastRefillBlockIndex = scheduleRow.StartBlockIndex;
            }
            else
            {
                // Calculate elapsed blocks since last refill
                var elapsed = blockIndex - lastRefillBlockIndex;

                if (elapsed < resetIntervalBlockRange)
                {
                    // Not enough time has passed for refill
                    currentTickets = infiniteTowerInfo.RemainingTickets;
                }
                else
                {
                    // Calculate how many full periods have passed
                    var periods = (int)(elapsed / resetIntervalBlockRange);
                    var desiredAdd = scheduleRow.DailyFreeTickets * periods;
                    var capacityLeft = System.Math.Max(0, scheduleRow.MaxTickets - infiniteTowerInfo.RemainingTickets);
                    var ticketsToAdd = System.Math.Min(capacityLeft, desiredAdd);

                    // Calculate current tickets (same as server logic)
                    currentTickets = infiniteTowerInfo.RemainingTickets + ticketsToAdd;

                    // Update reference point for progressed block range calculation
                    // (simulating what would happen on server)
                    lastRefillBlockIndex += periods * (long)resetIntervalBlockRange;
                }
            }

            // Calculate progressed block range since last refill
            // Use the updated lastRefillBlockIndex if refill occurred
            var progressedBlockRange = (blockIndex - lastRefillBlockIndex) % resetIntervalBlockRange;

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
            var accountAddress =
                Addresses.InfiniteTowerInfo.Derive($"{scheduleRow.InfiniteTowerId}");
            var state = await _agent.GetStateAsync(stateRootHash, accountAddress, _currentAvatarAddr.Value);
            if (state is Bencodex.Types.List serialized)
            {
                var infiniteTowerInfo = new InfiniteTowerInfo(serialized);
                _infiniteTowerInfoUpdatedBlockIndex = _agent.BlockIndex;
                return infiniteTowerInfo;
            }

            // If no state exists, create a new InfiniteTowerInfo
            return new InfiniteTowerInfo(_currentAvatarAddr.Value, scheduleRow.InfiniteTowerId, scheduleRow.DailyFreeTickets);
        }
    }
}
