using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public struct AuthorizedMiningPolicy
    {
        public AuthorizedMiningPolicy(
            long startIndex, long? endIndex, long interval, ISet<Address> miners)
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
            else if (miners.Count == 0)
            {
                throw new ArgumentException(
                    $"Set {nameof(miners)} cannot be empty.");
            }

            Miners = miners;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Interval = interval;
        }

        public ISet<Address> Miners { get; private set; }

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

        public static AuthorizedMiningPolicy Mainnet => new AuthorizedMiningPolicy()
        {
            StartIndex = 0,
            EndIndex = 3_153_600,
            Interval = 50,
            Miners = new[]
            {
                new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
                new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
                new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
                new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
            }.ToImmutableHashSet(),
        };
    }
}
