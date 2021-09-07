using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;

namespace Lib9c
{
    public class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
    {
        public IComparer<IBlockExcerpt> CanonicalChainComparer { get; } = new TotalDifficultyComparer();

        public IAction BlockAction { get; } = new RewardGold();

        public InvalidBlockException ValidateNextBlock(
            BlockChain<PolymorphicAction<ActionBase>> blocks,
            Block<PolymorphicAction<ActionBase>> nextBlock
        )
        {
            return null;
        }

        public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
        {
            return blocks.Count > 0 ? 1 : 0;
        }

        public int MaxTransactionsPerBlock { get; } = int.MaxValue;

        public int GetMaxBlockBytes(long index) => int.MaxValue;

        public bool DoesTransactionFollowsPolicy(
            Transaction<PolymorphicAction<ActionBase>> transaction,
            BlockChain<PolymorphicAction<ActionBase>> blockChain
        ) =>
            true;

        public HashAlgorithmType GetHashAlgorithm(long index) =>
            HashAlgorithmType.Of<SHA256>();

        public int GetMaxTransactionsPerSignerPerBlock(long index) => MaxTransactionsPerBlock;
    }
}
