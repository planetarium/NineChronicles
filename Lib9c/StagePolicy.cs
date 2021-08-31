namespace Nekoyume.BlockChain
{
    using Libplanet;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Tx;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using NCAction = Libplanet.Action.PolymorphicAction<Action.ActionBase>;

    public class StagePolicy : IStagePolicy<NCAction>
    {
        private readonly VolatileStagePolicy<NCAction> _impl;
        private readonly ConcurrentDictionary<Address, SortedList<Transaction<NCAction>, TxId>> _txs;
        private readonly ConcurrentDictionary<TxId, bool> _outdated;
        private readonly int _quotaPerSigner;

        public StagePolicy(TimeSpan txLifeTime, int quotaPerSigner)
        {
            _txs = new ConcurrentDictionary<Address, SortedList<Transaction<NCAction>, TxId>>();
            _outdated = new ConcurrentDictionary<TxId, bool>();
            _quotaPerSigner = quotaPerSigner;
            _impl = (txLifeTime == default)
                ? new VolatileStagePolicy<NCAction>()
                : new VolatileStagePolicy<NCAction>(txLifeTime);
        }

        public Transaction<NCAction> Get(BlockChain<NCAction> blockChain, TxId id, bool includeUnstaged)
            => _impl.Get(blockChain, id, includeUnstaged);

        public void Ignore(BlockChain<NCAction> blockChain, TxId id)
        {
            _outdated.TryRemove(id, out var _);
            _impl.Ignore(blockChain, id);
        }

        public bool Ignores(BlockChain<NCAction> blockChain, TxId id)
            => !_outdated.ContainsKey(id) && _impl.Ignores(blockChain, id);

        public IEnumerable<Transaction<NCAction>> Iterate(BlockChain<NCAction> blockChain)
            => _impl.Iterate(blockChain);

        public void Stage(BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            Address signer = transaction.Signer;
            SortedList<Transaction<NCAction>, TxId> txsForSigner = _txs.GetOrAdd(
                signer,
                _ => new SortedList<Transaction<NCAction>, TxId>(
                    new TxComparer()
                )
            );

            lock (txsForSigner)
            {
                txsForSigner[transaction] = transaction.Id;
                _impl.Stage(blockChain, transaction);

                if (txsForSigner.Count > _quotaPerSigner)
                {
                    TxId lastId = txsForSigner.Values.Last();
                    Unstage(blockChain, lastId);
                    _outdated[lastId] = true;
                }
            }
        }

        public void Unstage(BlockChain<NCAction> blockChain, TxId id)
        {
            if (Get(blockChain, id, includeUnstaged: true) is Transaction<NCAction> tx &&
                _txs.TryGetValue(tx.Signer, out SortedList<Transaction<NCAction>, TxId> l))
            {
                lock (l)
                {
                    _impl.Unstage(blockChain, id);
                    l.Remove(tx);
                    if (l.Count == 0)
                    {
                        _txs.TryRemove(tx.Signer, out var _);
                    }
                    _outdated.TryRemove(id, out var _);
                }
            }
        }

        private class TxComparer : IComparer<Transaction<NCAction>>
        {
            public int Compare(Transaction<NCAction> x, Transaction<NCAction> y)
            {
                if (x.Nonce < y.Nonce)
                {
                    return -1;
                }
                else if (x.Nonce > y.Nonce)
                {
                    return 1;
                }
                else if (x.Timestamp < y.Timestamp)
                {
                    return -1;
                }
                else if (x.Timestamp > y.Timestamp)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
