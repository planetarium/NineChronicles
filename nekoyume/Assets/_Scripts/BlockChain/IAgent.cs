using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Helper;
using UniRx;

namespace Nekoyume.BlockChain
{
    public interface IAgent
    {
        Subject<long> BlockIndexSubject { get; }
        
        long BlockIndex { get; }
        
        PrivateKey PrivateKey { get; }

        Address Address { get; }

        ActionRenderer ActionRenderer { get; }

        void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(GameAction gameAction);

        IValue GetState(Address address);

    }
}
