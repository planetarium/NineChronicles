using System;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Assets;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
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

        BlockPolicySource BlockPolicySource { get; }

        BlockRenderer BlockRenderer { get; }

        ActionRenderer ActionRenderer { get; }

        int AppProtocolVersion { get; }

        Subject<BlockHash> BlockTipHashSubject { get; }

        BlockHash BlockTipHash { get; }

        void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(GameAction gameAction);

        IValue GetState(Address address);

        bool IsActionStaged(Guid actionId, out TxId txId);

        FungibleAssetValue GetBalance(Address address, Currency currency);
    }
}
