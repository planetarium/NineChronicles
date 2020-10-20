using System;
using System.Collections.Generic;
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
        private BlockChain<PolymorphicAction<ActionBase>> _chain;
        private Swarm<PolymorphicAction<ActionBase>> _swarm;

        public Address Address { get; }

        public async Task<Block<PolymorphicAction<ActionBase>>> MineBlockAsync(
            int txBatchSize,
            CancellationToken cancellationToken)
        {
            var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();

            var invalidTxs = txs;
            Block<PolymorphicAction<ActionBase>> block = null;
            try
            {
                block = await _chain.MineBlock(
                    Address,
                    DateTimeOffset.UtcNow,
                    cancellationToken: cancellationToken,
                    txBatchSize: txBatchSize);

                if (_swarm.Running)
                {
                    _swarm.BroadcastBlock(block);
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

        public Miner(BlockChain<PolymorphicAction<ActionBase>> chain, Swarm<PolymorphicAction<ActionBase>> swarm, PrivateKey privateKey)
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _swarm = swarm ?? throw new ArgumentNullException(nameof(swarm));

            Address = privateKey.PublicKey.ToAddress();
        }
    }
}
