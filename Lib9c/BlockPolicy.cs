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
        /// Whether to ignore or respect hardcoded policies. If this is turned off,
        /// older blocks pass some new rules by force. Therefore, on the mainnet,
        /// this should be turned off. This option is made mainly due to unit tests.
        /// Turning on this option can be useful for tests.
        /// </summary>
        internal readonly bool IgnoreHardcodedPolicies;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            int minTransactionsPerBlock,
            int maxBlockBytes,
            int maxGenesisBytes,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null)
            : this(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                maxBlockBytes: maxBlockBytes,
                maxGenesisBytes: maxGenesisBytes,
                ignoreHardcodedPolicies: false,
                permissionedMiningPolicy: BlockChain.PermissionedMiningPolicy.Mainnet,
                validateNextBlockTx: validateNextBlockTx,
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
            int maxBlockBytes,
            int maxGenesisBytes,
            bool ignoreHardcodedPolicies,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                maxBlockBytes: maxBlockBytes,
                maxGenesisBytes: maxGenesisBytes,
                validateNextBlockTx: validateNextBlockTx,
                canonicalChainComparer: new CanonicalChainComparer(null),
#pragma warning disable LAA1002
                hashAlgorithmGetter: HashAlgorithmTable.ToHashAlgorithmGetter(),
#pragma warning restore LAA1002
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
            ?? ValidateMinerPermission(nextBlock)
            ?? ValidateMinerAuthority(nextBlock)
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

            var prevIndex = IsTargetBlock(index - 1) ? index - 2 : index - 1;
            var beforePrevIndex = IsTargetBlock(prevIndex - 1) ? prevIndex - 2 : prevIndex - 1;

            if (beforePrevIndex > AuthorizedMinersState.ValidUntil)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            if (IsTargetBlock(index) || prevIndex <= 1 || beforePrevIndex <= 1)
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
            if (block.Transactions.Count <= 0 &&
                (IgnoreHardcodedPolicies || block.Index > 2_173_700))
            {
                return new BlockPolicyViolationException(
                    $"Block #{block.Index} {block.Hash} mined by {miner} should " +
                    "include at least one transaction.");
            }

            return null;
        }

        private BlockPolicyViolationException ValidateMinerPermission(Block<NCAction> block)
        {
            Address miner = block.Miner;

            if (!IgnoreHardcodedPolicies)
            {
                return null;
            }

            if (!(PermissionedMiningPolicy is PermissionedMiningPolicy policy))
            {
                return null;
            }

            if (block.Index < policy.Threshold)
            {
                return null;
            }

            if (policy.Miners.Contains(miner) && block.Transactions.Any(t => t.Signer.Equals(miner)))
            {
                return null;
            }

            return new BlockPolicyViolationException(
                $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.");
        }

        private BlockPolicyViolationException ValidateMinerAuthority(Block<NCAction> block)
        {
            Address miner = block.Miner;

            if (AuthorizedMinersState is null)
            {
                return null;
            }

            if (!IsTargetBlock(block.Index))
            {
                return null;
            }

            if (!AuthorizedMinersState.Miners.Contains(miner))
            {
                return new BlockPolicyViolationException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.");
            }

            // Authority should be proven through a no-op transaction (= txs with zero actions).
            // (For backward compatibility, blocks before 1,200,000th don't have to be proven.
            // Note that as of Feb 9, 2021, there are about 770,000+ blocks.)
            Transaction<NCAction>[] txs = block.Transactions.ToArray();
            if (!txs.Any(tx => tx.Signer.Equals(miner) && !tx.Actions.Any()) &&
                block.ProtocolVersion > 0 &&
                (IgnoreHardcodedPolicies || block.Index > 1_200_000))
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

            return null;
        }

        private bool IsTargetBlock(long blockIndex)
        {
            return blockIndex > 0
                   && blockIndex <= AuthorizedMinersState.ValidUntil
                   && blockIndex % AuthorizedMinersState.Interval == 0;
        }
    }
}
