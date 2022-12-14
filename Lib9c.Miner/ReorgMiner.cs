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
    /// <summary>
    /// Intentionally causes repetitive reorgs for debugging purpose.
    /// </summary>
    public class ReorgMiner
    {
        private readonly BlockChain<PolymorphicAction<ActionBase>> _mainChain;
        private readonly BlockChain<PolymorphicAction<ActionBase>> _subChain;
        private readonly Swarm<PolymorphicAction<ActionBase>> _mainSwarm;
        private readonly Swarm<PolymorphicAction<ActionBase>> _subSwarm;
        private readonly PrivateKey _privateKey;
        private readonly int _reorgInterval;

        public Address Address => _privateKey.ToAddress();

        public ReorgMiner(
            Swarm<PolymorphicAction<ActionBase>> mainSwarm,
            Swarm<PolymorphicAction<ActionBase>> subSwarm,
            PrivateKey privateKey,
            int reorgInterval)
        {
            _mainSwarm = mainSwarm ?? throw new ArgumentNullException(nameof(mainSwarm));
            _subSwarm = subSwarm ?? throw new ArgumentNullException(nameof(subSwarm));
            _mainChain = mainSwarm.BlockChain ?? throw new ArgumentNullException(nameof(mainSwarm.BlockChain));
            _subChain = subSwarm.BlockChain ?? throw new ArgumentNullException(nameof(subSwarm.BlockChain));
            _privateKey = privateKey;
            _reorgInterval = reorgInterval;
        }

        public async Task<(
                Block<PolymorphicAction<ActionBase>>? MainBlock,
                Block<PolymorphicAction<ActionBase>>? SubBlock)>
            MineBlockAsync(CancellationToken cancellationToken)
        {
            var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();

            var invalidTxs = txs;
            Block<PolymorphicAction<ActionBase>>? mainBlock = null;
            Block<PolymorphicAction<ActionBase>>? subBlock = null;
            try
            {
                mainBlock = await _mainChain.MineBlock(
                    _privateKey,
                    DateTimeOffset.UtcNow,
                    cancellationToken: cancellationToken);

                subBlock = await _subChain.MineBlock(
                    _privateKey,
                    DateTimeOffset.UtcNow,
                    cancellationToken: cancellationToken);

                if (_reorgInterval != 0 && subBlock.Index % _reorgInterval == 0)
                {
                    Log.Debug("Force reorg!");
                    _subSwarm.BroadcastBlock(subBlock);
                }

                if (_mainSwarm.Running)
                {
                    _mainSwarm.BroadcastBlock(mainBlock);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Mining was canceled due to change of tip.");
            }
            catch (InvalidTxException invalidTxException)
            {
                var invalidTx = _mainChain.GetTransaction(invalidTxException.TxId);

                Log.Debug($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                invalidTxs.Add(invalidTx);
            }
            catch (UnexpectedlyTerminatedActionException actionException)
            {
                if (actionException.TxId is TxId txId)
                {
                    Log.Debug(
                        $"Tx[{actionException.TxId}]'s action is invalid. mark to unstage. {actionException}");
                    invalidTxs.Add(_mainChain.GetTransaction(txId));
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
                    _mainChain.UnstageTransaction(invalidTx);
                    _subChain.UnstageTransaction(invalidTx);
                }
            }

            return (mainBlock, subBlock);
        }
    }
}
