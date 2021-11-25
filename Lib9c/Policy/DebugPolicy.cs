using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;

namespace Nekoyume.BlockChain.Policy
{
    public class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
    {
        public IComparer<IBlockExcerpt> CanonicalChainComparer { get; } = new TotalDifficultyComparer();

        public IAction BlockAction { get; } = new RewardGold();

        public TxPolicyViolationException ValidateNextBlockTx(
            BlockChain<PolymorphicAction<ActionBase>> blockChain,
            Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            return null;
        }

        public BlockPolicyViolationException ValidateNextBlock(
            BlockChain<PolymorphicAction<ActionBase>> blockChain,
            Block<PolymorphicAction<ActionBase>> nextBlock)
        {
            return null;
        }

        public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blockChain)
        {
            return blockChain.Count > 0 ? 1 : 0;
        }

        public long GetMaxBlockBytes(long index) => long.MaxValue;

        public HashAlgorithmType GetHashAlgorithm(long index) =>
            HashAlgorithmType.Of<SHA256>();

        public int GetMinTransactionsPerBlock(long index) => 0;

        public int GetMaxTransactionsPerBlock(long index) => int.MaxValue;

        public int GetMaxTransactionsPerSignerPerBlock(long index) => int.MaxValue;
    }
}
