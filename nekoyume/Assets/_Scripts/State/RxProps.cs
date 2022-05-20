using System;
using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State
{
    using UniRx;

    public static class RxProps
    {
        #region Arena

        private static readonly ReactiveProperty<(long bedinning, long end, long progress)>
            _arenaProgress = new ReactiveProperty<(long bedinning, long end, long progress)>();

        public static IReadOnlyReactiveProperty<(long bedinning, long end, long progress)>
            ArenaProgress => _arenaProgress;

        private static readonly ReactiveProperty<(long bedinning, long end, long progress)>
            _arenaTicketProgress = new ReactiveProperty<(long bedinning, long end, long progress)>();

        public static IReadOnlyReactiveProperty<(long bedinning, long end, long progress)>
            ArenaTicketProgress => _arenaTicketProgress;

        private static readonly AsyncUpdatableRxProp<ArenaInfo>
            _arenaInfo = new AsyncUpdatableRxProp<ArenaInfo>(UpdateArenaTicketCountAsync);

        private static long _arenaInfoUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<ArenaInfo>
            ArenaInfo => _arenaInfo;

        #endregion

        private static IAgent _agent;
        private static States _states;

        private static readonly List<IDisposable> _disposables = new List<IDisposable>();

        public static void Start(IAgent agent, States states)
        {
            Debug.Log($"{nameof(RxProps)} start");
            _agent = agent;
            _states = states;
            _disposables.DisposeAllAndClear();
            _agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(OnBlockIndex)
                .AddTo(_disposables);
        }

        public static void Stop()
        {
            Debug.Log($"{nameof(RxProps)} stop");
            _disposables.DisposeAllAndClear();
        }

        private static void OnBlockIndex(long blockIndex)
        {
            var gcs = _states.GameConfigState;
            var was = _states.WeeklyArenaState;
            UpdateArenaProgress(blockIndex, gcs, was);
            UpdateArenaTicketProgress(blockIndex, gcs, was);

            _arenaInfo.UpdateAsync().Forget();
        }

        private static void UpdateArenaProgress(
            long blockIndex,
            GameConfigState gcs,
            WeeklyArenaState was)
        {
            if (gcs is null || was is null)
            {
                return;
            }

            var beginningBlockIndex = blockIndex - blockIndex % gcs.WeeklyArenaInterval;
            var endBlockIndex = beginningBlockIndex + gcs.WeeklyArenaInterval;
            var progressBlockIndex = blockIndex - beginningBlockIndex;
            _arenaProgress.SetValueAndForceNotify((
                beginningBlockIndex,
                endBlockIndex,
                progressBlockIndex));
        }

        private static void UpdateArenaTicketProgress(
            long blockIndex,
            GameConfigState gcs,
            WeeklyArenaState was)
        {
            if (gcs is null || was is null)
            {
                return;
            }

            var beginningBlockIndex = was.ResetIndex;
            var endBlockIndex = beginningBlockIndex + gcs.DailyArenaInterval;
            var progressBlockIndex = blockIndex - beginningBlockIndex;
            _arenaTicketProgress.SetValueAndForceNotify((
                beginningBlockIndex,
                endBlockIndex,
                progressBlockIndex));
        }

        private static async UniTask<ArenaInfo> UpdateArenaTicketCountAsync(
            ArenaInfo previous)
        {
            if (_arenaInfoUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaInfoUpdatedBlockIndex = _agent.BlockIndex;

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            if (!currentAddress.HasValue)
            {
                return null;
            }

            var avatarAddress = currentAddress.Value;
            var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress.ToByteArray());
            var rawInfo = await UniTask.Run(async () =>
                await Game.Game.instance.Agent.GetStateAsync(infoAddress));
            if (!(rawInfo is Dictionary dictionary))
            {
                return null;
            }

            return new ArenaInfo(dictionary);
        }
    }
}
