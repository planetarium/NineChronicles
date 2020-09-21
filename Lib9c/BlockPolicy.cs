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
    public class BlockPolicy : IBlockPolicy<NCAction>
    {
        private readonly IBlockPolicy<NCAction> _impl;
        
        private readonly Func<Block<NCAction>, InvalidBlockException> _blockValidator;

        public BlockPolicy(
            IBlockPolicy<NCAction> impl, 
            Func<Block<NCAction>, InvalidBlockException> blockValidator
        )
        {
            _impl = impl;
            _blockValidator = blockValidator;
        }

        public IAction BlockAction => _impl.BlockAction;

        public bool DoesTransactionFollowsPolicy(
            Transaction<NCAction> transaction, 
            BlockChain<NCAction> blockChain
        ) => _impl.DoesTransactionFollowsPolicy(transaction, blockChain);

        public long GetNextBlockDifficulty(BlockChain<NCAction> blocks)
            => _impl.GetNextBlockDifficulty(blocks);

        public InvalidBlockException ValidateNextBlock(BlockChain<NCAction> blocks, Block<NCAction> nextBlock)
        {
            InvalidBlockException excFromValidator = _blockValidator(nextBlock);
            if (!(excFromValidator is null))
            {
                return excFromValidator;
            }

            return _impl.ValidateNextBlock(blocks, nextBlock);
        }
    }
}
