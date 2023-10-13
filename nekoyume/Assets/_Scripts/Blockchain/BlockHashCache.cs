using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Types.Blocks;

namespace Nekoyume.Blockchain
{
    public class BlockHashCache
    {
        private readonly int _capacity;

        private readonly int _evictCount;

        // TODO: fix stateRootHash to non-nullable with IBlockChainService.
        private readonly List<(long blockIndex, BlockHash blockHash, HashDigest<SHA256>? stateRootHash)> _cache;

        public BlockHashCache(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    capacity,
                    "Must be greater than 0."
                );
            }

            _capacity = capacity;
            _evictCount = Math.Max(1, capacity / 10);
            _cache = new List<(long blockIndex, BlockHash blockHash, HashDigest<SHA256>?)>(_capacity);
        }

        public void Add(long blockIndex, BlockHash blockHash, HashDigest<SHA256>? stateRootHash)
        {
            if (_cache.Count >= _capacity)
            {
                Evict();
            }

            _cache.Add((blockIndex, blockHash, stateRootHash));
        }

        public bool TryGet(
            long blockIndex,
            out BlockHash blockHash,
            out HashDigest<SHA256>? stateRootHash)
        {
            try
            {
                (_, blockHash, stateRootHash) = _cache.First(t => t.blockIndex == blockIndex);
                return true;
            }
            catch
            {
                blockHash = default;
                stateRootHash = default;
                return false;
            }
        }

        public bool TryGet(
            BlockHash blockHash,
            out long blockIndex,
            out HashDigest<SHA256>? stateRootHash)
        {
            try
            {
                (blockIndex, _, stateRootHash) = _cache.First(t => t.blockHash.Equals(blockHash));
                return true;
            }
            catch
            {
                blockIndex = default;
                stateRootHash = default;
                return false;
            }
        }

        public bool TryGet(
            HashDigest<SHA256> stateRootHash,
            out long blockIndex,
            out BlockHash blockHash)
        {
            try
            {
                (blockIndex, blockHash, _) = _cache.First(t => t.stateRootHash.Equals(stateRootHash));
                return true;
            }
            catch
            {
                blockIndex = default;
                blockHash = default;
                return false;
            }
        }

        public void Clear()
        {
            _cache.Clear();
        }

        private void Evict()
        {
            if (_cache.Count <= _evictCount)
            {
                _cache.Clear();
                return;
            }

            for (var i = 0; i < _evictCount; i++)
            {
                _cache.RemoveAt(0);
            }
        }
    }
}
