using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using System;
using System.Linq;
using Lib9c;
using Libplanet;
using Nekoyume.Model.State;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private readonly long _minimumDifficulty;
        private readonly long _difficultyBoundDivisor;
        private AuthorizedMinersState _authorizedMinersState;

        /// <summary>
        /// Whether to ignore or respect hardcoded block indices to make older
        /// blocks compatible with the latest rules.  If it's turned off
        /// (by default) older blocks pass some new rules by force.
        /// Therefore, on the mainnet this should be turned off.
        /// This option is made mainly due to unit tests.  Turning on this
        /// option can be useful for tests.
        /// </summary>
        internal readonly bool IgnoreHardcodedIndicesForBackwardCompatibility;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            int maxTransactionsPerBlock,
            int maxBlockBytes,
            int maxGenesisBytes,
            Func<Transaction<NCAction>, BlockChain<NCAction>, bool> doesTransactionFollowPolicy = null
        )
            : this(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                maxTransactionsPerBlock: maxTransactionsPerBlock,
                maxBlockBytes: maxBlockBytes,
                maxGenesisBytes: maxGenesisBytes,
                ignoreHardcodedIndicesForBackwardCompatibility: false,
                doesTransactionFollowPolicy: doesTransactionFollowPolicy
            )
        {
        }

        internal BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            int maxTransactionsPerBlock,
            int maxBlockBytes,
            int maxGenesisBytes,
            bool ignoreHardcodedIndicesForBackwardCompatibility,
            Func<Transaction<NCAction>, BlockChain<NCAction>, bool> doesTransactionFollowPolicy = null
        )
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
                maxTransactionsPerBlock: maxTransactionsPerBlock,
                maxBlockBytes: maxBlockBytes,
                maxGenesisBytes: maxGenesisBytes,
                doesTransactionFollowPolicy: doesTransactionFollowPolicy,
                canonicalChainComparer: new CanonicalChainComparer(
                    null,
                    TimeSpan.FromTicks(blockInterval.Ticks * 10))
            )
        {
            _minimumDifficulty = minimumDifficulty;
            _difficultyBoundDivisor = difficultyBoundDivisor;
            IgnoreHardcodedIndicesForBackwardCompatibility =
                ignoreHardcodedIndicesForBackwardCompatibility;
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

        public override InvalidBlockException ValidateNextBlock(
            BlockChain<NCAction> blocks,
            Block<NCAction> nextBlock
        ) =>
            ValidateBlock(nextBlock)
            ?? ValidateMinerAuthority(nextBlock)
            ?? base.ValidateNextBlock(blocks, nextBlock);

        public override long GetNextBlockDifficulty(BlockChain<NCAction> blocks)
        {
            if (AuthorizedMinersState is null)
            {
                return base.GetNextBlockDifficulty(blocks);
            }

            long index = blocks.Count;

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
                return base.GetNextBlockDifficulty(blocks);
            }

            if (IsTargetBlock(index) || prevIndex <= 1 || beforePrevIndex <= 1)
            {
                return _minimumDifficulty;
            }

            var prevBlock = blocks[prevIndex];
            var beforePrevBlock = blocks[beforePrevIndex];

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

        private InvalidBlockException ValidateBlock(Block<NCAction> block)
        {
            if (!(block.Miner is Address miner))
            {
                return null;
            }

            // As a temporary approach to prevent selfish mining (again), we add a new rule
            // disallowing blocks with less than 3 transactions.  This rule is applied since
            // 2,100,000th block.  (Note that as of Aug 4, 2021, there are about 2,060,000+ blocks.)
            // This rule is not applied to blocks (with proofs) made by authorized miners.
            if (block.Transactions.Count < 3 &&
                block.Index >= 2_150_000 &&
                !(AuthorizedMinersState is AuthorizedMinersState ams &&
                    block.Index <= ams.ValidUntil &&
                    block.Miner is Address m && ams.Miners.Contains(m) &&
                    block.Transactions.Any(tx => tx.Signer.Equals(m))))
            {
                return new InvalidMinerException(
                    $"The block #{block.Index} {block.Hash} (mined by {miner}) must " +
                    "include at least 3 transactions.",
                    miner
                );
            }

            // To prevent selfish mining, we define a consensus that blocks with no transactions are do not accepted. 
            // (For backward compatibility, blocks before 1,711,631th don't have to be proven.
            // Note that as of Jun 16, 2021, there are about 1,710,000+ blocks.)
            if (block.Transactions.Count <= 0 &&
                (IgnoreHardcodedIndicesForBackwardCompatibility || block.Index > 1_711_631))
            {
                return new InvalidMinerException(
                    $"The block #{block.Index} {block.Hash} (mined by {miner}) should " +
                    "include at least one transaction.",
                    miner
                );
            }

            return null;
        }

        private InvalidBlockException ValidateMinerAuthority(Block<NCAction> block)
        {
            if (AuthorizedMinersState is null)
            {
                return null;
            }

            if (!(block.Miner is Address miner))
            {
                return null;
            }

            if (!IsTargetBlock(block.Index))
            {
                return null;
            }

            if (!AuthorizedMinersState.Miners.Contains(miner))
            {
                return new InvalidMinerException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.",
                    miner
                );
            }


            // Authority should be proven through a no-op transaction (= txs with zero actions).
            // (For backward compatibility, blocks before 1,200,000th don't have to be proven.
            // Note that as of Feb 9, 2021, there are about 770,000+ blocks.)
            Transaction<NCAction>[] txs = block.Transactions.ToArray();
            if (!txs.Any(tx => tx.Signer.Equals(miner) && !tx.Actions.Any()) &&
                block.ProtocolVersion > 0 &&
                (IgnoreHardcodedIndicesForBackwardCompatibility || block.Index > 1_200_000))
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
                return new InvalidMinerException(
                    $"The block #{block.Index} {block.Hash}'s miner {miner} should be proven by " +
                    "including a no-op transaction by signed the same authority." + debug,
                    miner
                );
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
