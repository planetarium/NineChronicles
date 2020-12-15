using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Libplanet.Tx;
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

        BlockRenderer BlockRenderer { get; }

        ActionRenderer ActionRenderer { get; }

        int AppProtocolVersion { get; }

        Subject<HashDigest<SHA256>> BlockTipHashSubject { get; }

        HashDigest<SHA256> BlockTipHash { get; }

        void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(GameAction gameAction);

        IValue GetState(Address address);

        void SendException(Exception exc);

        bool IsActionStaged(Guid actionId, out TxId txId);

        FungibleAssetValue GetBalance(Address address, Currency currency);
    }
}
