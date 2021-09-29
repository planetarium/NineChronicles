using System;

namespace Nekoyume.BlockChain.Policy
{
    public struct MinTransactionsPerBlockPolicy
    {
        public MinTransactionsPerBlockPolicy(
            long startIndex, long? endIndex, int minTransactionsPerBlock)
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

            StartIndex = startIndex;
            EndIndex = endIndex;
            MinTransactionsPerBlock = minTransactionsPerBlock;
        }

        public long StartIndex { get; private set; }

        public long? EndIndex { get; private set; }

        public int MinTransactionsPerBlock { get; private set; }

        public bool IsTargetBlockIndex(long index)
        {
            return StartIndex <= index
                && (EndIndex is null
                    || (EndIndex is long endIndex && index <= endIndex));
        }

        public static MinTransactionsPerBlockPolicy Mainnet => new MinTransactionsPerBlockPolicy()
        {
            StartIndex = BlockPolicySource.MinTransactionsPerBlockHardcodedIndex,
            EndIndex = null,
            MinTransactionsPerBlock = BlockPolicySource.MinTransactionsPerBlock,
        };
    }
}
