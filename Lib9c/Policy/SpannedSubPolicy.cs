using System;
using System.Diagnostics.Contracts;

namespace Nekoyume.BlockChain.Policy
{
    public class SpannedSubPolicy<T>
    {
        public SpannedSubPolicy(
            long startIndex, long? endIndex, Func<long, bool> predicate, T value)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(startIndex),
                    actualValue: startIndex,
                    message: $"Start index must be non-negative: {startIndex}");
            }
            else if (endIndex is long ei && ei < startIndex)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(endIndex),
                    actualValue: endIndex,
                    message: $"Non-null end index must not be less than start index: " +
                        $"{{{nameof(startIndex)}: {startIndex}, {nameof(endIndex)}: {endIndex}}}");
            }

            StartIndex = startIndex;
            EndIndex = endIndex;
            Predicate = predicate ?? (index => true);
            Value = value;
        }

        public SpannedSubPolicy(long startIndex, long? endIndex, T value)
            : this(startIndex, endIndex, null, value)
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
                && (Indefinite || (EndIndex is long endIndex && index <= endIndex));
        }

        [Pure]
        public bool IsTargetIndex(long index)
        {
            return IsTargetRange(index) && Predicate(index);
        }

        public long StartIndex { get; }

        public long? EndIndex { get; }

        public Func<long, bool> Predicate { get; }

        public T Value { get; }

        public bool Indefinite => EndIndex is null;
    }
}
