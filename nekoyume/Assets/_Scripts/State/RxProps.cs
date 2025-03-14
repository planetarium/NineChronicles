using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Security.Cryptography;
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

            NcDebug.Log("[RxProps] Start");
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

            StartEvent();
        }

        public static void Stop()
        {
            NcDebug.Log($"{nameof(RxProps)} stop");
            _disposables.DisposeAllAndClear();
        }

        public static async UniTask SelectAvatarAsync(
            int avatarIndexToSelect,
            HashDigest<SHA256> stateRootHash,
            AvatarState avatarState,
            bool forceNewSelection = false)
        {
            await States.Instance.SelectAvatarAsync(
                avatarIndexToSelect,
                stateRootHash,
                avatarState,
                forceNewSelection: forceNewSelection);
            await UniTask.WhenAll(
                EventDungeonInfo.UpdateAsync(stateRootHash),
                WorldBossStates.Set(
                    stateRootHash,
                    _agent.BlockIndex,
                    _states.CurrentAvatarState.address),
                UniTask.RunOnThreadPool(States.Instance.InitAvatarBalancesAsync).ToObservable().ObserveOnMainThread().ToUniTask(),
                States.Instance.InitItemSlotStates());

            // 아레나서비스에 등록하는시점에 cp계산이 필요해서 ItemSlot States까지 전부 갱신된다음 아레나데이터 초기화
            await InitializeArena();
        }

        public static async UniTask SelectAvatarAsync(
            int avatarIndexToSelect,
            HashDigest<SHA256> stateRootHash,
            bool forceNewSelection = false)
        {
            await States.Instance.SelectAvatarAsync(
                avatarIndexToSelect,
                stateRootHash,
                forceNewSelection: forceNewSelection);
            await UniTask.WhenAll(
                EventDungeonInfo.UpdateAsync(stateRootHash),
                WorldBossStates.Set(
                    stateRootHash,
                    _agent.BlockIndex,
                    _states.CurrentAvatarState.address),
                UniTask.RunOnThreadPool(States.Instance.InitAvatarBalancesAsync).ToObservable().ObserveOnMainThread().ToUniTask(),
                States.Instance.InitItemSlotStates());

            // 아레나서비스에 등록하는시점에 cp계산이 필요해서 ItemSlot States까지 전부 갱신된다음 아레나데이터 초기화
            await InitializeArena();
        }

        private static void OnBlockIndex(long blockIndex)
        {
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
            OnAvatarChangedEvent();
        }
    }
}
