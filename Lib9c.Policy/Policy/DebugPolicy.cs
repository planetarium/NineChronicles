using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;

namespace Nekoyume.BlockChain.Policy
{
    public class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
    {
        public DebugPolicy(long blockDifficulty)
        {
            _blockDifficulty = blockDifficulty;
        }

        public IComparer<IBlockExcerpt> CanonicalChainComparer { get; } = new TotalDifficultyComparer();

        public IAction BlockAction { get; } = new RewardGold();

        private readonly long _blockDifficulty;

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
            return blockChain.Count > 0 ? _blockDifficulty : 0;
        }

        public long GetMaxTransactionsBytes(long index) => long.MaxValue;

        public HashAlgorithmType GetHashAlgorithm(long index) =>
            HashAlgorithmType.Of<SHA256>();

        public int GetMinTransactionsPerBlock(long index) => 0;

        public int GetMaxTransactionsPerBlock(long index) => int.MaxValue;

        public int GetMaxTransactionsPerSignerPerBlock(long index) => int.MaxValue;

        public int GetMinBlockProtocolVersion(long index) => 0;

        public IImmutableSet<Currency> NativeTokens => ImmutableHashSet<Currency>.Empty;
    }
}
