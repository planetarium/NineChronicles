using System.Collections.Generic;
using Libplanet.Blocks;

namespace Nekoyume.Blockchain
{
    public class BlockHashCache
    {
        private readonly Dictionary<long, BlockHash> _dict;
        private readonly Queue<long> _keys;
        public int MaxCapacity { get; }

        public BlockHashCache(int maxCapacity)
        {
            _dict = new Dictionary<long, BlockHash>(maxCapacity);
            _keys = new Queue<long>(maxCapacity);
            MaxCapacity = maxCapacity;
        }

        public void Add(long blockIndex, BlockHash blockHash)
        {
            if (_dict.ContainsKey(blockIndex))
            {
                return;
            }

            if (_dict.Count >= MaxCapacity)
            {
                var key = _keys.Dequeue();
                _dict.Remove(key);
            }

            _dict.Add(blockIndex, blockHash);
            _keys.Enqueue(blockIndex);
        }

        public bool TryGetBlockHash(long blockIndex, out BlockHash blockHash)
        {
            return _dict.TryGetValue(blockIndex, out blockHash);
        }

        public bool Remove(long blockIndex)
        {
            return _dict.Remove(blockIndex);
        }

        public void Clear()
        {
            _dict.Clear();
            _keys.Clear();
        }
    }
}
