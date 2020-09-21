using Lib9c;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using System;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    public class BlockPolicy : BlockPolicy<NCAction>
    {
        private readonly Func<Block<NCAction>, InvalidBlockException> _blockValidator;

        public BlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            long minimumDifficulty,
            int difficultyBoundDivisor,
            Func<Transaction<NCAction>, BlockChain<NCAction>, bool> doesTransactionFollowPolicy = null,
            Func<Block<NCAction>, InvalidBlockException> blockValidator)
            : base(
                blockAction,
                blockInterval,
                minimumDifficulty,
                difficultyBoundDivisor,
                doesTransactionFollowPolicy)
        {
            _blockValidator = blockValidator;
        }

        public override InvalidBlockException ValidateNextBlock(BlockChain<NCAction> blocks, Block<NCAction> nextBlock)
            => _blockValidator(nextBlock);
    }
}
