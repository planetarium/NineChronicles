using System;

namespace Nekoyume.BlockChain.Policy
{
    public struct MaxTransactionsPerSignerPerBlockPolicy
    {
        public MaxTransactionsPerSignerPerBlockPolicy(
            long startIndex, long? endIndex, int maxTransactionsPerSignerPerBlock)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"Value of {nameof(startIndex)} must be non-negative: {startIndex}");
            }
            else if (endIndex is long ei && ei < startIndex)
            {
                throw new ArgumentOutOfRangeException(
                    $"Non-null {nameof(endIndex)} cannot be less than {nameof(startIndex)}.");
            }
            else if (maxTransactionsPerSignerPerBlock < 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"Value of {nameof(maxTransactionsPerSignerPerBlock)} must be non-negative: " +
                    $"{maxTransactionsPerSignerPerBlock}");
            }

            StartIndex = startIndex;
            EndIndex = endIndex;
            MaxTransactionsPerSignerPerBlock = maxTransactionsPerSignerPerBlock;
        }

        public long StartIndex { get; private set; }

        public long? EndIndex { get; private set; }

        public int MaxTransactionsPerSignerPerBlock { get; private set; }

        public bool IsTargetBlockIndex(long index)
        {
            return StartIndex <= index
                && (EndIndex is null
                    || (EndIndex is long endIndex && index <= endIndex));
        }

        public static MaxTransactionsPerSignerPerBlockPolicy Mainnet => new MaxTransactionsPerSignerPerBlockPolicy()
        {
            StartIndex = BlockPolicySource.MaxTransactionsPerSignerPerBlockHardcodedIndex,
            EndIndex = null,
            MaxTransactionsPerSignerPerBlock = BlockPolicySource.MaxTransactionsPerSignerPerBlock,
        };
    }
}
