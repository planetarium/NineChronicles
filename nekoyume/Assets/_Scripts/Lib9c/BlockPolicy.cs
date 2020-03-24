using System;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;

namespace Nekoyume.BlockChain
{
    public class BlockPolicy
    {
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold {Gold = 1};

            public InvalidBlockException ValidateNextBlock(
                BlockChain<PolymorphicAction<ActionBase>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock
            )
            {
                return null;
            }

            public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
            {
                return blocks.Tip is null ? 0 : 1;
            }

            public bool DoesTransactionFollowsPolicy(
                Transaction<PolymorphicAction<ActionBase>> transaction
            ) =>
                true;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public static IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(
            int miniumDifficulty
        )
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 1 },
                BlockInterval,
                miniumDifficulty,
                2048
            );
#endif
        }
    }
}
