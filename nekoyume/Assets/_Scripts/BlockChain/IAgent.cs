using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Assets;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
using Nekoyume.Helper;
using Nekoyume.Model.State;
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

        Subject<BlockHash> BlockTipHashSubject { get; }

        BlockHash BlockTipHash { get; }

        IObservable<(Transaction<PolymorphicAction<ActionBase>> tx, List<PolymorphicAction<ActionBase>> actions)>
            OnMakeTransaction { get; }

        IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(ActionBase actionBase);

        IValue GetState(Address address);
        Task<IValue> GetStateAsync(Address address);

        void SendException(Exception exc);

        bool TryGetTxId(Guid actionId, out TxId txId);

        UniTask<bool> IsTxStagedAsync(TxId txId);

        FungibleAssetValue GetBalance(Address address, Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(Address address, Currency currency);

        Task<Dictionary<Address, AvatarState>> GetAvatarStates(IEnumerable<Address> addressList);

        Task<Dictionary<Address, IValue>> GetStateBulk(IEnumerable<Address> addressList);
    }
}
