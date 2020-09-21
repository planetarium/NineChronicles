using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using System;
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

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            Func<Transaction<NCAction>, BlockChain<NCAction>, bool> doesTransactionFollowPolicy = null
        ) : base(
                blockAction,
                blockInterval,
                minimumDifficulty,
                difficultyBoundDivisor,
                doesTransactionFollowPolicy)
        {
            _minimumDifficulty = minimumDifficulty;
            _difficultyBoundDivisor = difficultyBoundDivisor;
        }

        public AuthorizedMinersState AuthorizedMinersState { get; set; }

        public override InvalidBlockException ValidateNextBlock(BlockChain<NCAction> blocks, Block<NCAction> nextBlock)
        {
            InvalidBlockException e = ValidateMinerAuthority(nextBlock);
            return e ?? base.ValidateNextBlock(blocks, nextBlock);
        }

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

            bool minedByAuthorities = AuthorizedMinersState.Miners.Contains(miner);

            if (minedByAuthorities)
            {
                return null;
            }

            return new InvalidMinerException(
                $"The given block[{block}] isn't mined by authorities.",
                miner
            );
        }

        private bool IsTargetBlock(long blockIndex)
        {
            return blockIndex > 0
                   && blockIndex <= AuthorizedMinersState.ValidUntil
                   && blockIndex % AuthorizedMinersState.Interval == 0;
        }
    }
}
