using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.BlockChain
{
    public interface IAgent
    {
        Subject<long> BlockIndexSubject { get; }
        
        long BlockIndex { get; }

        Address Address { get; }

        void Initialize(Action<bool> callback);

        void EnqueueAction(GameAction gameAction);

        IValue GetState(Address address);
    }
}
