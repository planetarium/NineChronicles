using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Tx;
using Nekoyume.Action;
using Serilog;

namespace Nekoyume.BlockChain
{
    public class Miner
    {
        private readonly BlockChain<PolymorphicAction<ActionBase>> _chain;
        private readonly Swarm<PolymorphicAction<ActionBase>> _swarm;
        private readonly PrivateKey _privateKey;
        // TODO we must justify it.
        private static readonly ImmutableHashSet<Address> _bannedAccounts = new[]
        {
            new Address("de96aa7702a7a1fd18ee0f84a5a0c7a2c28ec840"),
            new Address("153281c93274bEB9726A03C33d3F19a8D78ad805"),
            new Address("7035AA8B7F9fB5db026fb843CbB21C03dd278502"),
            new Address("52393Ea89DF0E58152cbFE673d415159aa7B9dBd"),
            new Address("2D1Db6dBF1a013D648Efd16d85B4079dCF88B4CC"),
            new Address("dE30E00917B583305f14aD21Eafc70f1b183b779"),
            new Address("B892052f1E10bf700143dd9bEcd81E31CD7f7095"),

            new Address("C0a90FC489738A1153F793A3272A91913aF3956b"),
            new Address("b8D7bD4394980dcc2579019C39bA6b41cb6424E1"),
            new Address("555221D1CEA826C55929b8A559CA929574f7C6B3"),
            new Address("B892052f1E10bf700143dd9bEcd81E31CD7f7095"),
        }.ToImmutableHashSet();

        public bool AuthorizedMiner { get; }

        public Address Address => _privateKey.ToAddress();

        public Transaction<PolymorphicAction<ActionBase>> StageProofTransaction()
        {
            // We assume authorized miners create no transactions at all except for
            // proof transactions.  Without the assumption, nonces for proof txs become
            // much complicated to determine.
            var proof = Transaction<PolymorphicAction<ActionBase>>.Create(
                _chain.GetNextTxNonce(_privateKey.ToAddress()),
                _privateKey,
                _chain.Genesis.Hash,
                new PolymorphicAction<ActionBase>[0]
            );
            _chain.StageTransaction(proof);
            return proof;
        }

        public async Task<Block<PolymorphicAction<ActionBase>>> MineBlockAsync(
            int maxTransactions,
            CancellationToken cancellationToken)
        {
            var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();
            var invalidTxs = txs;

            Block<PolymorphicAction<ActionBase>> block = null;
            try
            {
                if (AuthorizedMiner)
                {
                    _chain
                        .GetStagedTransactionIds()
                        .Select(txid => _chain.GetTransaction(txid)).ToList()
                        .ForEach(tx => _chain.UnstageTransaction(tx));
                }

                IEnumerable<Transaction<PolymorphicAction<ActionBase>>> bannedTxs = _chain.GetStagedTransactionIds()
                    .Select(txId => _chain.GetTransaction(txId))
                    .Where(tx => _bannedAccounts.Contains(tx.Signer));
                foreach (Transaction<PolymorphicAction<ActionBase>> tx in bannedTxs)
                {
                    _chain.UnstageTransaction(tx);
                }

                // All miner needs proof in permissioned mining.
                StageProofTransaction();
                block = await _chain.MineBlock(
                    Address,
                    DateTimeOffset.UtcNow,
                    cancellationToken: cancellationToken,
                    maxTransactions: maxTransactions,
                    append: false);

                _chain.Append(block);
                if (_swarm is Swarm<PolymorphicAction<ActionBase>> s && s.Running)
                {
                    s.BroadcastBlock(block);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Mining was canceled due to change of tip.");
            }
            catch (InvalidTxException invalidTxException)
            {
                var invalidTx = _chain.GetTransaction(invalidTxException.TxId);

                Log.Debug($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                invalidTxs.Add(invalidTx);
            }
            catch (UnexpectedlyTerminatedActionException actionException)
            {
                if (actionException.TxId is TxId txId)
                {
                    Log.Debug(
                        $"Tx[{actionException.TxId}]'s action is invalid. mark to unstage. {actionException}");
                    invalidTxs.Add(_chain.GetTransaction(txId));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"exception was thrown. {ex}");
            }
            finally
            {
#pragma warning disable LAA1002
                foreach (var invalidTx in invalidTxs)
#pragma warning restore LAA1002
                {
                    _chain.UnstageTransaction(invalidTx);
                }

            }

            return block;
        }

        public Miner(
            BlockChain<PolymorphicAction<ActionBase>> chain,
            Swarm<PolymorphicAction<ActionBase>> swarm,
            PrivateKey privateKey,
            bool authorizedMiner
        )
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _swarm = swarm;
            _privateKey = privateKey;
            AuthorizedMiner = authorizedMiner;
        }
    }
}
