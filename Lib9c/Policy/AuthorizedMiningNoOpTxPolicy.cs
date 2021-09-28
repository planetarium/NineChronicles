using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public struct AuthorizedMiningNoOpTxPolicy
    {
        public AuthorizedMiningNoOpTxPolicy(
            long startIndex, long? endIndex, long interval)
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
            else if (interval <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"Value of {nameof(interval)} must be positive: {interval}");
            }

            StartIndex = startIndex;
            EndIndex = endIndex;
            Interval = interval;
        }

        public long StartIndex { get; private set; }

        public long? EndIndex { get; private set; }

        public long Interval { get; private set; }

        public bool IsTargetBlockIndex(long index)
        {
            return index % Interval == 0
                && StartIndex <= index
                && (EndIndex is null
                    || (EndIndex is long endIndex && index <= endIndex));
        }

        public static AuthorizedMiningNoOpTxPolicy Mainnet
        {
            get
            {
                AuthorizedMiningPolicy authorizedMiningPolicy = AuthorizedMiningPolicy.Mainnet;
                return new AuthorizedMiningNoOpTxPolicy()
                {
                    StartIndex = BlockPolicySource.AuthorizedMiningNoOpTxHardcodedIndex,
                    EndIndex = authorizedMiningPolicy.EndIndex,
                    Interval = authorizedMiningPolicy.Interval,
                };
            }
        }
    }
}
