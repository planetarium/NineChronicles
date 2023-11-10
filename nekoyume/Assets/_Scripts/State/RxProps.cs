using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume.State
{
    using UniRx;

    public static partial class RxProps
    {
        private static IAgent _agent;
        private static States _states;
        private static TableSheets _tableSheets;

        private static readonly List<IDisposable> _disposables = new();

        private static Address? _currentAvatarAddr;

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

            Debug.Log("[RxProps] Start");
            _agent = agent;
            _states = states;
            _tableSheets = tableSheets;

            _disposables.DisposeAllAndClear();
            _agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(OnBlockIndex)
                .AddTo(_disposables);

            ReactiveAvatarState.Address
                .Subscribe(OnAvatarChanged)
                .AddTo(_disposables);

            StartArena();
            StartEvent();
        }

        public static void Stop()
        {
            Debug.Log($"{nameof(RxProps)} stop");
            _disposables.DisposeAllAndClear();
        }

        public static async UniTask SelectAvatarAsync(
            int avatarIndexToSelect,
            bool forceNewSelection = false)
        {
            await States.Instance.SelectAvatarAsync(
                avatarIndexToSelect,
                forceNewSelection: forceNewSelection);
            await UniTask.WhenAll(
                ArenaInfoTuple.UpdateAsync(),
                EventDungeonInfo.UpdateAsync(),
                WorldBossStates.Set(States.Instance.CurrentAvatarState.address),
                States.Instance.InitAvatarBalancesAsync(),
                States.Instance.InitRuneStates(),
                States.Instance.InitRuneSlotStates(),
                States.Instance.InitItemSlotStates());
        }

        private static void OnBlockIndex(long blockIndex)
        {
            OnBlockIndexArena(blockIndex);
            OnBlockIndexEvent(blockIndex);
        }

        private static void OnAvatarChanged(Address avatarAddr)
        {
            if (_currentAvatarAddr.HasValue &&
                _currentAvatarAddr.Value.Equals(avatarAddr))
            {
                return;
            }

            _currentAvatarAddr = avatarAddr;
            OnAvatarChangedArena();
            OnAvatarChangedEvent();
        }
    }
}
