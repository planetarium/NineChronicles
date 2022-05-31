using System;
using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using UnityEngine;

namespace Nekoyume.State
{
    using UniRx;

    public static class RxProps
    {
        #region Arena

        private static readonly ReactiveProperty<(long beginning, long end, long progress)>
            _arenaTicketProgress = new ReactiveProperty<(long beginning, long end, long progress)>();

        public static IReadOnlyReactiveProperty<(long beginning, long end, long progress)>
            ArenaTicketProgress => _arenaTicketProgress;

        private static readonly AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple =
                new AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>(
                    UpdateArenaInformationAsync);

        private static long _arenaInformationUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;
        
        private static readonly AsyncUpdatableRxProp<(Address avatarAddress, int score)[]>
            _arenaParticipantsOrderedWithScore = new AsyncUpdatableRxProp<(Address avatarAddress, int score)[]>(
                Array.Empty<(Address avatarAddress, int score)>(),
                UpdateArenaParticipantsOrderedWithScoreAsync);

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;

        public static IReadOnlyAsyncUpdatableRxProp<(Address avatarAddress, int score)[]>
            ArenaParticipantsOrderedWithScore => _arenaParticipantsOrderedWithScore;

        #endregion

        private static IAgent _agent;
        private static States _states;
        private static TableSheets _tableSheets;

        private static readonly List<IDisposable> _disposables = new List<IDisposable>();

        public static void Start(IAgent agent, States states, TableSheets tableSheets)
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (states is null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (tableSheets is null)
            {
                throw new ArgumentNullException(nameof(tableSheets));
            }

            Debug.Log($"{nameof(RxProps)} start");
            _agent = agent;
            _states = states;
            _tableSheets = tableSheets;
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
            UpdateArenaTicketProgress(blockIndex);

            _arenaInfoTuple.UpdateAsync().Forget();
        }

        private static void UpdateArenaTicketProgress(
            long blockIndex)
        {
            var beginningBlockIndex = _states.WeeklyArenaState.ResetIndex;
            var endBlockIndex = beginningBlockIndex + _states.GameConfigState.DailyArenaInterval;
            var progressBlockIndex = blockIndex - beginningBlockIndex;
            _arenaTicketProgress.SetValueAndForceNotify((
                beginningBlockIndex,
                endBlockIndex,
                progressBlockIndex));
        }

        private static async UniTask<(ArenaInformation current, ArenaInformation next)>
            UpdateArenaInformationAsync((ArenaInformation current, ArenaInformation next) previous)
        {
            if (_arenaInformationUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaInformationUpdatedBlockIndex = _agent.BlockIndex;

            var avatarAddress = States.Instance.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return previous;
            }

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var sheet = _tableSheets.ArenaSheet;
            if (!sheet.TryGetCurrentRound(blockIndex, out var currentRoundData))
            {
                Debug.Log($"Failed to get current round data. block index({blockIndex})");
                return previous;
            }

            var currentArenaInfoAddress = ArenaInformation.DeriveAddress(
                avatarAddress.Value,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var nextArenaInfoAddress = sheet.TryGetNextRound(blockIndex, out var nextRoundData)
                ? ArenaInformation.DeriveAddress(
                    avatarAddress.Value,
                    nextRoundData.ChampionshipId,
                    nextRoundData.Round)
                : default;
            var dict = await Game.Game.instance.Agent.GetStateBulk(
                new[]
                {
                    currentArenaInfoAddress,
                    nextArenaInfoAddress
                }
            );
            var currentArenaInfo =
                dict.TryGetValue(currentArenaInfoAddress, out var currentValue) &&
                currentValue is List currentList
                    ? new ArenaInformation(currentList)
                    : null;
            var nextArenaInfo =
                dict.TryGetValue(nextArenaInfoAddress, out var nextValue) &&
                nextValue is List nextList
                    ? new ArenaInformation(nextList)
                    : null;
            return (currentArenaInfo, nextArenaInfo);
        }

        private static async UniTask<(Address avatarAddress, int score)[]>
            UpdateArenaParticipantsOrderedWithScoreAsync((Address avatarAddress, int score)[] previous)
        {
            if (_arenaParticipantsOrderedWithScoreUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }
            
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentRoundData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var address = ArenaParticipants.DeriveAddress(
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var participants = await _agent.GetStateAsync(address);
            if (!(participants is List list))
            {
                Debug.Log($"Failed to get {nameof(ArenaParticipants)} with {address.ToHex()}");
                return previous;
            }
            
            
            
            return previous;
            
            
        }
    }
}
