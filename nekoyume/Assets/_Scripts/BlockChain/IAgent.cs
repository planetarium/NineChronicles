using System;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Renderer;
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

        BlockRenderer BlockRenderer { get; }

        ActionRenderer ActionRenderer { get; }

        int AppProtocolVersion { get; }

        Subject<HashDigest<SHA256>> BlockHashSubject { get; }

        void Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(GameAction gameAction);

        IValue GetState(Address address);

    }
}
