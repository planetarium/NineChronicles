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
        private readonly int _quotaPerSigner;

        public StagePolicy(TimeSpan txLifeTime, int quotaPerSigner)
        {
            _txs = new ConcurrentDictionary<Address, SortedList<Transaction<NCAction>, TxId>>();
            _quotaPerSigner = quotaPerSigner;
            _impl = (txLifeTime == default)
                ? new VolatileStagePolicy<NCAction>()
                : new VolatileStagePolicy<NCAction>(txLifeTime);
        }

        public Transaction<NCAction> Get(BlockChain<NCAction> blockChain, TxId id, bool includeUnstaged)
            => _impl.Get(blockChain, id, includeUnstaged);

        public long GetNextTxNonce(Address address, long minedTxs)
            => _impl.GetNextTxNonce(address, minedTxs);

        public void Ignore(BlockChain<NCAction> blockChain, TxId id)
            => _impl.Ignore(blockChain, id);

        public bool Ignores(BlockChain<NCAction> blockChain, TxId id)
            => _impl.Ignores(blockChain, id);

        public IEnumerable<Transaction<NCAction>> Iterate()
        {
            var txsPerSigner = new Dictionary<Address, SortedSet<Transaction<NCAction>>>();
            foreach (Transaction<NCAction> tx in _impl.Iterate())
            {
                if (!txsPerSigner.TryGetValue(tx.Signer, out var s))
                {
                    txsPerSigner[tx.Signer] = s = new SortedSet<Transaction<NCAction>>(new TxComparer());
                }

                s.Add(tx);
                if (s.Count > _quotaPerSigner)
                {
                    s.Remove(s.Max);
                }
            }

#pragma warning disable LAA1002 // DictionariesOrSetsShouldBeOrderedToEnumerate
            return txsPerSigner.Values.SelectMany(i => i);
#pragma warning restore LAA1002 // DictionariesOrSetsShouldBeOrderedToEnumerate
        }

        public void Stage(BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
            => _impl.Stage(blockChain, transaction);

        public void Unstage(BlockChain<NCAction> blockChain, TxId id)
            => _impl.Unstage(blockChain, id);

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
