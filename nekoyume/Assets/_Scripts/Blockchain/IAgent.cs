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

        HashDigest<SHA256> BlockTipStateRootHash { get; }

        IObservable<(Transaction tx, List<ActionBase> actions)>
            OnMakeTransaction { get; }

        IEnumerator Initialize(
            CommandLineOptions options,
            PrivateKey privateKey,
            Action<bool> callback
        );

        void EnqueueAction(ActionBase actionBase);

        IValue GetState(Address accountAddress, Address address);
        IValue GetState(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address);

        Task<IValue> GetStateAsync(Address accountAddress, Address address);
        Task<IValue> GetStateAsync(long blockIndex, Address accountAddress, Address address);
        Task<IValue> GetStateAsync(BlockHash blockHash, Address accountAddress, Address address);
        Task<IValue> GetStateAsync(HashDigest<SHA256> stateRootHash, Address accountAddress, Address address);

        void SendException(Exception exc);

        bool TryGetTxId(Guid actionId, out TxId txId);

        UniTask<bool> IsTxStagedAsync(TxId txId);

        FungibleAssetValue GetBalance(Address address, Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(
            Address address,
            Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(
            long blockIndex,
            Address address,
            Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(
            BlockHash blockHash,
            Address address,
            Currency currency);

        Task<FungibleAssetValue> GetBalanceAsync(
            HashDigest<SHA256> stateRootHash,
            Address address,
            Currency currency);

        Task<AgentState> GetAgentStateAsync(Address address);

        Task<AgentState> GetAgentStateAsync(
            long blockIndex,
            Address address);

        Task<AgentState> GetAgentStateAsync(
            HashDigest<SHA256> stateRootHash,
            Address address);

        Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            IEnumerable<Address> addressList);

        Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            long blockIndex,
            IEnumerable<Address> addressList);

        Task<Dictionary<Address, AvatarState>> GetAvatarStatesAsync(
            HashDigest<SHA256> stateRootHash,
            IEnumerable<Address> addressList);

        Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            Address accountAddress,
            IEnumerable<Address> addressList);

        Task<Dictionary<Address, IValue>> GetStateBulkAsync(
            HashDigest<SHA256> stateRootHash,
            Address accountAddress,
            IEnumerable<Address> addressList);

        Task<Dictionary<Address, IValue>> GetSheetsAsync(IEnumerable<Address> addressList);
    }
}
