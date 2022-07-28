using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State
{
    using UniRx;

    public static partial class RxProps
    {
        private static IAgent _agent;
        private static States _states;
        private static TableSheets _tableSheets;

        private static readonly List<IDisposable> _disposables = new List<IDisposable>();

        private static Address? _currentAvatarAddress;

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

            StartArena();
        }

        public static void Stop()
        {
            Debug.Log($"{nameof(RxProps)} stop");
            _disposables.DisposeAllAndClear();
        }

        private static void OnBlockIndex(long blockIndex)
        {
            OnBlockIndexArena(blockIndex);
        }
    }
}