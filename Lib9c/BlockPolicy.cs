using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Lib9c;
using Libplanet;
using Nekoyume.Model.State;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private static readonly Dictionary<long, HashAlgorithmType> HashAlgorithmTable =
            new Dictionary<long, HashAlgorithmType> { [0] = HashAlgorithmType.Of<SHA256>() };
        private readonly long _minimumDifficulty;
        private readonly long _difficultyBoundDivisor;
        private AuthorizedMinersState _authorizedMinersState;

        /// <summary>
        /// <para>
        /// Whether to ignore or respect hardcoded policies.
        /// </para>
        /// <para>
        /// There are several policies where each policy only applies after its corresponding
        /// hardcoded index.  Turning on this option ignores these hard coded indices and
        /// applies said policies starting from index 0, the gensis.
        /// </para>
        /// <para>
        /// This is purely for unit testing and should be set to false for production.
        /// </para>
        /// </summary>
        internal readonly bool IgnoreHardcodedPolicies;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            IComparer<IBlockExcerpt> canonicalChainComparer,
            HashAlgorithmGetter hashAlgorithmGetter,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<long, int> getMaxBlockBytes = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null)
            : this(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                ignoreHardcodedPolicies: false,
                permissionedMiningPolicy: BlockChain.PermissionedMiningPolicy.Mainnet,
                canonicalChainComparer: canonicalChainComparer,
                hashAlgorithmGetter: hashAlgorithmGetter,
                validateNextBlockTx: validateNextBlockTx,
                getMaxBlockBytes: getMaxBlockBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
        }

        internal BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            bool ignoreHardcodedPolicies,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            IComparer<IBlockExcerpt> canonicalChainComparer,
            HashAlgorithmGetter hashAlgorithmGetter,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<long, int> getMaxBlockBytes = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                validateNextBlockTx: validateNextBlockTx,
                canonicalChainComparer: canonicalChainComparer,
                hashAlgorithmGetter: hashAlgorithmGetter,
                getMaxBlockBytes: getMaxBlockBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
            _minimumDifficulty = minimumDifficulty;
            _difficultyBoundDivisor = difficultyBoundDivisor;
            IgnoreHardcodedPolicies = ignoreHardcodedPolicies;
            PermissionedMiningPolicy = permissionedMiningPolicy;
        }

        public AuthorizedMinersState AuthorizedMinersState
        {
            get => _authorizedMinersState;
            set
            {
                _authorizedMinersState = value;
                ((CanonicalChainComparer)CanonicalChainComparer).AuthorizedMinersState = value;
            }
        }

        public PermissionedMiningPolicy? PermissionedMiningPolicy { get; }

        public override BlockPolicyViolationException ValidateNextBlock(
            BlockChain<NCAction> blockChain,
            Block<NCAction> nextBlock
        ) =>
            CheckTxCount(nextBlock)
                ?? ValidateMinerAuthority(nextBlock)
                ?? ValidateMinerPermission(nextBlock)
                ?? base.ValidateNextBlock(blockChain, nextBlock);

        public override long GetNextBlockDifficulty(BlockChain<NCAction> blockChain)
        {
            if (AuthorizedMinersState is null)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            long index = blockChain.Count;

            if (index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"index must be 0 or more, but its index is {index}.");
            }

            if (index <= 1)
            {
                return index == 0 ? 0 : _minimumDifficulty;
            }

            var prevIndex = IsTargetBlockIndex(index - 1) ? index - 2 : index - 1;
            var beforePrevIndex = IsTargetBlockIndex(prevIndex - 1) ? prevIndex - 2 : prevIndex - 1;

            if (beforePrevIndex > AuthorizedMinersState.ValidUntil)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            if (IsTargetBlockIndex(index) || prevIndex <= 1 || beforePrevIndex <= 1)
            {
                return _minimumDifficulty;
            }

            var prevBlock = blockChain[prevIndex];
            var beforePrevBlock = blockChain[beforePrevIndex];

            DateTimeOffset beforePrevTimestamp = beforePrevBlock.Timestamp;
            DateTimeOffset prevTimestamp = prevBlock.Timestamp;
            TimeSpan timeDiff = prevTimestamp - beforePrevTimestamp;
            long timeDiffMilliseconds = (long)timeDiff.TotalMilliseconds;
            const long minimumMultiplier = -99;
            long multiplier = 1 - timeDiffMilliseconds / (long)BlockInterval.TotalMilliseconds;
            multiplier = Math.Max(multiplier, minimumMultiplier);

            var prevDifficulty = prevBlock.Difficulty;
            var offset = prevDifficulty / _difficultyBoundDivisor;
            long nextDifficulty = prevDifficulty + (offset * multiplier);

            return Math.Max(nextDifficulty, _minimumDifficulty);
        }

        private BlockPolicyViolationException CheckTxCount(Block<NCAction> block)
        {
            if (!(block.Miner is Address miner))
            {
                return null;
            }

            // To prevent selfish mining, we define a consensus that blocks with no transactions are do not accepted.
            // (For backward compatibility, blocks before 2,175,000th don't have to be proven.
            // Note that as of Aug 19, 2021, there are about 2,171,000+ blocks.)
            if (block.Transactions.Count <= 0)
            {
                if (IgnoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} mined by {miner} should " +
                        "include at least one transaction. (Forced failure)");
                }
                else if (block.Index > 2_173_700)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} mined by {miner} should " +
                        "include at least one transaction.");
                }
            }

            return null;
        }

        private BlockPolicyViolationException ValidateMinerPermission(Block<NCAction> block)
        {
            Address miner = block.Miner;

            // If no permission policy is given, pass validation by default.
            if (!(PermissionedMiningPolicy is PermissionedMiningPolicy policy))
            {
                return null;
            }

            // Predicate for permission validity.
            if (!policy.Miners.Contains(miner) || !block.Transactions.Any(t => t.Signer.Equals(miner)))
            {
                if (IgnoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.  "
                            + "(Forced failure)");
                }
                else if (block.Index >= policy.Threshold)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.");
                }
            }

            return null;
        }

        private BlockPolicyViolationException ValidateMinerAuthority(Block<NCAction> block)
        {
            Address miner = block.Miner;

            // If AuthorizedMinorState is null, do not check miner authority.
            if (AuthorizedMinersState is null)
            {
                return null;
            }
            // Authorized minor validity is only checked for certain indices.
            else if (!IsTargetBlockIndex(block.Index))
            {
                return null;
            }
            // Otherwise, block's miner should be one of the authorized miners.
            else if (!AuthorizedMinersState.Miners.Contains(miner))
            {
                return new BlockPolicyViolationException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.");
            }

            // Authority should be proven through a no-op transaction (= txs with zero actions).
            // (For backward compatibility, blocks before 1,200,000th don't have to be proven.
            // Note that as of Feb 9, 2021, there are about 770,000+ blocks.)
            Transaction<NCAction>[] txs = block.Transactions.ToArray();
            if (!txs.Any(tx => tx.Signer.Equals(miner) && !tx.Actions.Any())
                    && block.ProtocolVersion > 0)
            {
                if (IgnoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash}'s miner {miner} should be proven by "
                            + "including a no-op transaction by signed the same authority.  "
                            + "(Forced failure)");
                }
                else if (block.Index > 1_200_000)
                {
#if DEBUG
                    string debug =
                        "  Note that there " +
                        (txs.Length == 1 ? "is a transaction:" : $"are {txs.Length} transactions:") +
                        txs.Select((tx, i) =>
                                $"\n    {i}. {tx.Actions.Count} actions; signed by {tx.Signer}")
                            .Aggregate(string.Empty, (a, b) => a + b);
#else
                    const string debug = "";
#endif
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash}'s miner {miner} should be proven by " +
                        "including a no-op transaction by signed the same authority." + debug);
                }
            }

            return null;
        }

        private bool IsTargetBlockIndex(long blockIndex)
        {
            return blockIndex > 0
                && blockIndex <= AuthorizedMinersState.ValidUntil
                && blockIndex % AuthorizedMinersState.Interval == 0;
        }
    }
}
