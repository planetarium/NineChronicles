using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using Nekoyume.Model.State;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private readonly long _minimumDifficulty;
        private readonly long _difficultyStability;
        private readonly Func<BlockChain<NCAction>, AuthorizedMinersState>
            _getAuthorizedMinersState;
        private readonly Func<BlockChain<NCAction>, Address, long, bool> _isAllowedToMine;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long difficultyStability,
            long minimumDifficulty,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            IComparer<IBlockExcerpt> canonicalChainComparer,
            HashAlgorithmGetter hashAlgorithmGetter,
            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
                validateNextBlock = null,
            Func<long, int> getMaxBlockBytes = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null,
            Func<BlockChain<NCAction>, AuthorizedMinersState> getAuthorizedMinersState = null,
            Func<BlockChain<NCAction>, Address, long, bool> isAllowedToMine = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                difficultyStability: difficultyStability,
                minimumDifficulty: minimumDifficulty,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                canonicalChainComparer: canonicalChainComparer,
                hashAlgorithmGetter: hashAlgorithmGetter,
                getMaxBlockBytes: getMaxBlockBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
            _minimumDifficulty = minimumDifficulty;
            _difficultyStability = difficultyStability;
            _getAuthorizedMinersState = getAuthorizedMinersState;
            _isAllowedToMine = isAllowedToMine;
        }

        public override long GetNextBlockDifficulty(BlockChain<NCAction> blockChain)
        {
            long index = blockChain.Count;

            if (index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"index must be 0 or more, but its index is {index}.");
            }
            else if (index <= 1)
            {
                return index == 0 ? 0 : _minimumDifficulty;
            }
            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            // Authorized minor validity is only checked for certain indices.
            else if (GetAuthorizedMinersState(blockChain) is null)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            var prevIndex = IsAuthorizedMiningBlockIndex(blockChain, index - 1)
                ? index - 2
                : index - 1;
            var beforePrevIndex = IsAuthorizedMiningBlockIndex(blockChain, prevIndex - 1)
                ? prevIndex - 2
                : prevIndex - 1;

            if (beforePrevIndex > GetAuthorizedMinersState(blockChain).ValidUntil)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            if (IsAuthorizedMiningBlockIndex(blockChain, index)
                || prevIndex <= 1
                || beforePrevIndex <= 1)
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
            var offset = prevDifficulty / _difficultyStability;
            long nextDifficulty = prevDifficulty + (offset * multiplier);

            return Math.Max(nextDifficulty, _minimumDifficulty);
        }

        public bool IsAuthorizedMiningBlockIndex(BlockChain<NCAction> blockChain, long index)
        {
            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            return index > 0
                && GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams
                && index <= ams.ValidUntil
                && index % ams.Interval == 0;
        }

        public AuthorizedMinersState GetAuthorizedMinersState(BlockChain<NCAction> blockChain) =>
            _getAuthorizedMinersState(blockChain);

        public bool IsAllowedToMine(BlockChain<NCAction> blockChain, Address miner, long index) =>
            _isAllowedToMine(blockChain, miner, index);
    }
}
