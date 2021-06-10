using System;
using System.Collections.Generic;
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
                if (!AuthorizedMiner)
                {
                    block = await _chain.MineBlock(
                        Address,
                        DateTimeOffset.UtcNow,
                        cancellationToken: cancellationToken,
                        maxTransactions: maxTransactions,
                        append: false);
                }
                else
                {
                    Transaction<PolymorphicAction<ActionBase>> authProof = StageProofTransaction();
                    block = Block<PolymorphicAction<ActionBase>>.Mine(
                        _chain.Tip.Index + 1,
                        _chain.Policy.GetNextBlockDifficulty(_chain),
                        _chain.Tip.TotalDifficulty,
                        Address,
                        _chain.Tip.Hash,
                        DateTimeOffset.UtcNow,
                        new[] { authProof },
                        Block<PolymorphicAction<ActionBase>>.CurrentProtocolVersion,
                        cancellationToken);

                    if (_chain.Policy is BlockPolicy policy)
                    {
                        List<Address> miners = policy.AuthorizedMinersState.Miners.OrderBy(miner => miner).ToList();
                        int index = miners.IndexOf(Address);
                        int interval = policy.BlockInterval.Seconds;
                        if (interval > 0)
                        {
                            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            int modulo = (interval * (index + 1)) - (int)(currentTime % (interval * miners.Count));
                            int delay = modulo < 0
                                ? modulo + (interval * miners.Count)
                                : modulo;
                            Thread.Sleep(delay * 1000);
                        }
                    }
                }

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
