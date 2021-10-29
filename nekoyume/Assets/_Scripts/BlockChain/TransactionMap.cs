using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Tx;

namespace Nekoyume.BlockChain
{
    public class TransactionMap
    {
        private readonly int _size;

        private readonly ConcurrentQueue<KeyValuePair<Guid, TxId>> _queue =
            new ConcurrentQueue<KeyValuePair<Guid, TxId>>();

        public TransactionMap(int size)
        {
            _size = size;
        }

        public bool TryGetValue(Guid key, out TxId value)
        {
            if (!_queue.Any(kv => kv.Key.Equals(key)))
            {
                return false;
            }

            value = _queue.FirstOrDefault(kv => kv.Key.Equals(key)).Value;
            return true;
        }

        // FIXME: Should prevent duplicated item?
        public void TryAdd(Guid key, TxId value)
        {
            _queue.Enqueue(new KeyValuePair<Guid, TxId>(key, value));
            if (_queue.Count > _size)
            {
                _queue.TryDequeue(out _);
            }
        }
    }
}
