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
        private readonly long _difficultyBoundDivisor;
        private readonly Func<BlockChain<NCAction>, AdminState> _getAdminState;
        private readonly Func<BlockChain<NCAction>, AuthorizedMinersState>
            _getAuthorizedMinersState;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
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
            Func<BlockChain<NCAction>, AdminState> getAdminState = null,
            Func<BlockChain<NCAction>, AuthorizedMinersState> getAuthorizedMinersState = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: difficultyBoundDivisor,
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
            _difficultyBoundDivisor = difficultyBoundDivisor;
            _getAdminState = getAdminState;
            _getAuthorizedMinersState = getAuthorizedMinersState;
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

            var prevIndex = IsAuthorizedBlockIndex(blockChain, index - 1) ? index - 2 : index - 1;
            var beforePrevIndex = IsAuthorizedBlockIndex(blockChain, prevIndex - 1) ? prevIndex - 2 : prevIndex - 1;

            if (beforePrevIndex > GetAuthorizedMinersState(blockChain).ValidUntil)
            {
                return base.GetNextBlockDifficulty(blockChain);
            }

            if (IsAuthorizedBlockIndex(blockChain, index) || prevIndex <= 1 || beforePrevIndex <= 1)
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

        public bool IsAuthorizedBlockIndex(BlockChain<NCAction> blockChain, long index)
        {
            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            return index > 0
                && GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams
                && index <= ams.ValidUntil
                && index % ams.Interval == 0;
        }

        public AdminState GetAdminState(BlockChain<NCAction> blockChain) =>
            _getAdminState(blockChain);

        public AuthorizedMinersState GetAuthorizedMinersState(BlockChain<NCAction> blockChain) =>
            _getAuthorizedMinersState(blockChain);
    }
}
