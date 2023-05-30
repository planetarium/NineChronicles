using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Consensus;
using Libplanet.Crypto;
using Libplanet.Tx;
using Serilog;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// This is only for single node.
    /// Do not use this for production.
    /// </summary>
    public class Proposer
    {
        private readonly BlockChain<NCAction> _chain;
        private readonly PrivateKey _privateKey;

        public Block? ProposeBlockAsync(CancellationToken cancellationToken)
        {
            var txs = new HashSet<Transaction>();
            var invalidTxs = txs;

            Block? block = null;
            try
            {
                var lastCommit = _chain.GetBlockCommit(_chain.Tip.Hash);
                block = _chain.ProposeBlock(
                    _privateKey,
                    lastCommit: lastCommit);
                BlockCommit? commit = block.Index > 0
                    ? new BlockCommit(
                        block.Index,
                        0,
                        block.Hash,
                        ImmutableArray<Vote>.Empty
                            .Add(new VoteMetadata(
                                block.Index,
                                0,
                                block.Hash,
                                DateTimeOffset.UtcNow,
                                _privateKey.PublicKey,
                                VoteFlag.PreCommit).Sign(_privateKey)))
                    : null;
                _chain.Append(block, commit);
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
            catch (InvalidBlockCommitException ibce)
            {
                Log.Debug($"Proposed {nameof(BlockCommit)} is invalid. {ibce}");
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

        public Proposer(
            BlockChain<NCAction> chain,
            PrivateKey privateKey
        )
        {
            _chain = chain ?? throw new ArgumentNullException(nameof(chain));
            _privateKey = privateKey;
        }
    }
}
