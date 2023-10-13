#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;

namespace Nekoyume.Blockchain
{
    public class BlockchainCache
    {
        private class BlockchainData
        {
            /// <summary>
            /// block index of cached data.
            /// </summary>
            public long? BlockIndex;

            /// <summary>
            /// block hash of cached data.
            /// </summary>
            public BlockHash? BlockHash;

            /// <summary>
            /// state root hash of cached data.
            /// </summary>
            public HashDigest<SHA256>? StateRootHash;
        }

        private class BalanceData : BlockchainData
        {
            /// <summary>
            /// cached balance. it can be null.
            /// </summary>
            public FungibleAssetValue? Balance;
        }

        private class StateData : BlockchainData
        {
            /// <summary>
            /// cached state. it can be null.
            /// </summary>
            public IValue? State;
        }

        private readonly int _balanceCapacity;
        private readonly int _balanceEvictCount;
        private readonly Queue<(Address address, Currency currency)> _balanceKeyCache;
        private readonly Dictionary<Address, Dictionary<Currency, BalanceData>> _balanceDict;

        public BlockchainCache(int balanceCapacity)
        {
            if (balanceCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(balanceCapacity),
                    balanceCapacity,
                    "Must be greater than 0.");
            }

            _balanceCapacity = balanceCapacity;
            _balanceEvictCount = Math.Max(1, balanceCapacity / 10);
            _balanceKeyCache = new Queue<(Address, Currency)>(_balanceCapacity);
            _balanceDict = new Dictionary<Address, Dictionary<Currency, BalanceData>>();
        }

        public void Add(
            Address address,
            FungibleAssetValue balance,
            long? blockIndex = null,
            BlockHash? blockHash = null,
            HashDigest<SHA256>? stateRootHash = null)
        {
            if (blockIndex == null && blockHash == null && stateRootHash == null)
            {
                throw new ArgumentException(
                    "At least one of blockIndex, blockHash, stateRootHash must be specified.");
            }

            if (_balanceKeyCache.Count >= _balanceCapacity)
            {
                EvictBalance();
            }

            var data = new BalanceData
            {
                Balance = balance,
                BlockIndex = blockIndex,
                BlockHash = blockHash,
                StateRootHash = stateRootHash,
            };
            _balanceKeyCache.Enqueue((address, balance.Currency));
            if (!_balanceDict.ContainsKey(address))
            {
                _balanceDict[address] = new Dictionary<Currency, BalanceData>();
            }

            _balanceDict[address][balance.Currency] = data;
        }

        #region TryGetBalance

        public bool TryGetBalance(
            Address address,
            Currency currency,
            out FungibleAssetValue? balance,
            out long? blockIndex,
            out BlockHash? blockHash,
            out HashDigest<SHA256>? stateRootHash)
        {
            if (_balanceDict.TryGetValue(address, out var dict) &&
                dict.TryGetValue(currency, out var info))
            {
                balance = info.Balance;
                blockIndex = info.BlockIndex;
                blockHash = info.BlockHash;
                stateRootHash = info.StateRootHash;
                return true;
            }

            balance = null;
            blockIndex = null;
            blockHash = null;
            stateRootHash = null;
            return false;
        }

        public bool TryGetBalance(
            Address address,
            Currency currency,
            out FungibleAssetValue? balance) =>
            TryGetBalance(address, currency, out balance, out _, out _, out _);

        public bool TryGetBalance(
            long blockIndex,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance,
            out BlockHash? blockHash,
            out HashDigest<SHA256>? stateRootHash)
        {
            if (_balanceDict.TryGetValue(address, out var dict) &&
                dict.TryGetValue(currency, out var info) &&
                info.BlockIndex.HasValue &&
                info.BlockIndex == blockIndex)
            {
                balance = info.Balance;
                blockHash = info.BlockHash;
                stateRootHash = info.StateRootHash;
                return true;
            }

            balance = null;
            blockHash = null;
            stateRootHash = null;
            return false;
        }

        public bool TryGetBalance(
            long blockIndex,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance) =>
            TryGetBalance(blockIndex, address, currency, out balance, out _, out _);

        public bool TryGetBalance(
            BlockHash blockHash,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance,
            out long? blockIndex,
            out HashDigest<SHA256>? stateRootHash)
        {
            if (_balanceDict.TryGetValue(address, out var dict) &&
                dict.TryGetValue(currency, out var info) &&
                info.BlockHash.HasValue &&
                info.BlockHash.Value.Equals(blockHash))
            {
                balance = info.Balance;
                blockIndex = info.BlockIndex;
                stateRootHash = info.StateRootHash;
                return true;
            }

            balance = null;
            blockIndex = default;
            stateRootHash = null;
            return false;
        }

        public bool TryGetBalance(
            BlockHash blockHash,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance) =>
            TryGetBalance(blockHash, address, currency, out balance, out _, out _);

        public bool TryGetBalance(
            HashDigest<SHA256> stateRootHash,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance,
            out long? blockIndex,
            out BlockHash? blockHash)
        {
            if (_balanceDict.TryGetValue(address, out var dict) &&
                dict.TryGetValue(currency, out var info) &&
                info.StateRootHash.HasValue &&
                info.StateRootHash.Value.Equals(stateRootHash))
            {
                balance = info.Balance;
                blockIndex = info.BlockIndex;
                blockHash = info.BlockHash;
                return true;
            }

            balance = null;
            blockIndex = default;
            blockHash = null;
            return false;
        }

        public bool TryGetBalance(
            HashDigest<SHA256> stateRootHash,
            Address address,
            Currency currency,
            out FungibleAssetValue? balance) =>
            TryGetBalance(stateRootHash, address, currency, out balance, out _, out _);

        #endregion

        public void ClearBalance()
        {
            _balanceKeyCache.Clear();
            _balanceDict.Clear();
        }

        private void EvictBalance()
        {
            for (var i = 0; i < _balanceEvictCount; i++)
            {
                if (!_balanceKeyCache.TryDequeue(out var key))
                {
                    break;
                }

                if (_balanceDict.TryGetValue(key.address, out var dict))
                {
                    dict.Remove(key.currency);
                    if (dict.Count == 0)
                    {
                        _balanceDict.Remove(key.address);
                    }
                }
            }
        }
    }
}
