using System;
using System.Diagnostics.Contracts;

namespace Nekoyume.BlockChain.Policy
{
    public class SpannedSubPolicy<T>
    {
        /// <summary>
        /// Class for storing binding arguments for <see cref="VariableSubPolicy{T}"/>.
        /// One can think of this as a sparse list where a designated value is "stored" for
        /// any index between start index and end index (inclusive) satisfying
        /// <paramref name="predicate"/> condition.
        /// </summary>
        /// <param name="startIndex">Start index of the range.</param>
        /// <param name="endIndex">End index of the range, inclusive.</param>
        /// <param name="predicate">Additional index filtering predicate.</param>
        /// <param name="value">Value stored.</param>
        /// <exception cref="ArgumentOutOfRangeException">If an invalid value is given for either
        /// <paramref name="startIndex"/> or <paramref name="endIndex"/>.</exception>
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
                    message: $"Non-null end index must not be less than start index.");
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
