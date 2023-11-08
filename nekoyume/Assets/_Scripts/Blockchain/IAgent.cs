#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.Blockchain
{
    public interface IAgent
    {
        Subject<long> BlockIndexSubject { get; }

        long BlockIndex { get; }

        PrivateKey PrivateKey { get; }

        Address Address { get; }

        BlockRenderer BlockRenderer { get; }

        ActionRenderer ActionRenderer { get; }

        Subject<BlockHash> BlockTipHashSubject { get; }

        BlockHash BlockTipHash { get; }

        IObservable<(Transaction tx, List<ActionBase> actions)>
            OnMakeTransaction { get; }

        IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(ActionBase actionBase);

        IValue GetState(Address address);
        IValue GetState(Address address, HashDigest<SHA256> stateRootHash);

        Task<IValue> GetStateAsync(Address address, long? blockIndex = null);
        Task<IValue> GetStateAsync(Address address, BlockHash blockHash);
        Task<IValue> GetStateAsync(Address address, HashDigest<SHA256> stateRootHash);

        void SendException(Exception exc);

        bool TryGetTxId(Guid actionId, out TxId txId);

        UniTask<bool> IsTxStagedAsync(TxId txId);

        FungibleAssetValue GetBalance(Address address, Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency,
            long? blockIndex = null);

        Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency,
            BlockHash blockHash);

        Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency,
            HashDigest<SHA256> stateRootHash);

        Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList,
            long? blockIndex = null);

        Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList,
            HashDigest<SHA256> stateRootHash);

        Task<Dictionary<Address, IValue>> GetStateBulkAsync(IEnumerable<Address> addressList);

        Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            IEnumerable<Address> addressList,
            HashDigest<SHA256> stateRootHash);
    }
}
