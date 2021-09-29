using System;
using System.Diagnostics.Contracts;

namespace Nekoyume.BlockChain.Policy
{
    public class SpannedSubPolicy<T> : Tuple<long, long?, long, T>
    {
        public SpannedSubPolicy(long startIndex, long? endIndex, long interval, T value)
            : base(startIndex, endIndex, interval, value)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"Start index must be non-negative: {startIndex}");
            }
            else if (endIndex is long ei && ei < startIndex)
            {
                throw new ArgumentOutOfRangeException(
                    $"Non-null end index must not be less than start index: {startIndex}, {endIndex}");
            }
            else if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(
                    $"Interval must be positive: {interval}");
            }
        }

        public SpannedSubPolicy(long startIndex, long? endIndex, T value)
            : this(startIndex, endIndex, 1, value)
        {
        }

        public SpannedSubPolicy(long startIndex, T value)
            : this(startIndex, null, value)
        {
        }

        public SpannedSubPolicy(T value)
            : this(0, value)
        {
        }

        [Pure]
        public bool IsTargetRange(long index)
        {
            return StartIndex <= index
                && (EndIndex is null || (EndIndex is long endIndex && index <= endIndex));
        }

        [Pure]
        public bool IsTargetIndex(long index)
        {
            return IsTargetRange(index) && (Interval == 1 || index % Interval == 0);
        }

        public long StartIndex => Item1;

        public long? EndIndex => Item2;

        public long Interval => Item3;

        public T Value => Item4;

        public bool Indefinite => EndIndex is null;
    }
}
