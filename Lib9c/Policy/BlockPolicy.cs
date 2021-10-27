using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private readonly Func<BlockChain<NCAction>, long> _getNextBlockDifficulty;
        private readonly Func<Address, long, bool> _isAllowedToMine;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long difficultyStability,
            long minimumDifficulty,
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
            Func<BlockChain<NCAction>, long> getNextBlockDifficulty = null,
            Func<Address, long, bool> isAllowedToMine = null)
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
            _getNextBlockDifficulty = getNextBlockDifficulty;
            _isAllowedToMine = isAllowedToMine;
        }

        public override long GetNextBlockDifficulty(BlockChain<NCAction> blockChain) =>
             _getNextBlockDifficulty(blockChain);

        public bool IsAllowedToMine(Address miner, long index) =>
            _isAllowedToMine(miner, index);
    }
}
