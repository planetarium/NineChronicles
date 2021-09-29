using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Nekoyume.BlockChain.Policy
{
    public class VariableSubPolicy<T>
    {
        private T _defaultValue;
        private ImmutableList<SpannedSubPolicy<T>> _spannedSubPolicies;

        private VariableSubPolicy(T defaultValue)
        {
            _defaultValue = defaultValue;
            _spannedSubPolicies = ImmutableList<SpannedSubPolicy<T>>.Empty;

            Validate();
        }

        private VariableSubPolicy(
            VariableSubPolicy<T> variableSubPolicy, SpannedSubPolicy<T> spannedSubPolicy)
        {
            if (variableSubPolicy is null || spannedSubPolicy is null)
            {
                throw new NullReferenceException(
                    $"Both arguments {nameof(variableSubPolicy)} and {nameof(spannedSubPolicy)} " +
                    "must not be null.");
            }

            List<SpannedSubPolicy<T>> spannedSubPolicies =
                variableSubPolicy.SpannedSubPolicies.ToList();
            if (spannedSubPolicies.Count > 0)
            {
                SpannedSubPolicy<T> lastSpannedSubPolicy = spannedSubPolicies[-1];

                // If spannedSubPolicies.StartIndex <= lastSpannedSubPolicy.StartIndex
                // an exception will be automatically thrown when trying to create
                // a new SpannedSubPolicy<T> below.
                if (!(lastSpannedSubPolicy.EndIndex is long endIndex) ||
                    endIndex >= spannedSubPolicy.StartIndex)
                {
                    lastSpannedSubPolicy = new SpannedSubPolicy<T>(
                        lastSpannedSubPolicy.StartIndex,
                        spannedSubPolicy.StartIndex - 1,
                        lastSpannedSubPolicy.Value);
                    spannedSubPolicies[-1] = lastSpannedSubPolicy;
                }
            }

            spannedSubPolicies.Add(spannedSubPolicy);

            _defaultValue = variableSubPolicy.DefaultValue;
            _spannedSubPolicies = spannedSubPolicies.ToImmutableList();

            Validate();
        }

        /// <summary>
        /// Creates a new subpolicy with an additional <see cref="SpannedSubPolicy{T}"/> added.
        /// </summary>
        /// <param name="spannedSubPolicy">New <see cref="SpannedSubPolicy{T}"/> to add.</param>
        /// <returns>
        /// A new <see cref="VariableSubPolicy{T}"/> instance with
        /// <paramref name="spannedSubPolicy"/> added at the end.
        /// </returns>
        /// <remarks>
        /// Last spanned subpolicy will be cut short and adjusted accordingly before
        /// adding <paramref name="spannedSubPolicy"/> if <paramref name="spannedSubPolicy"/>
        /// overlaps with the last one.
        /// </remarks>
        [Pure]
        public VariableSubPolicy<T> Add(SpannedSubPolicy<T> spannedSubPolicy)
        {
            return new VariableSubPolicy<T>(this, spannedSubPolicy);
        }

        /// <summary>
        /// Checks if a <see cref="VariableSubPolicy{T}"/> instance is valid.
        /// Should be called inside every constructor at the end.
        /// </summary>
        private void Validate()
        {
            SpannedSubPolicy<T> prev = null;
            foreach (SpannedSubPolicy<T> next in SpannedSubPolicies)
            {
                if (prev is SpannedSubPolicy<T> _prev)
                {
                    if (_prev.EndIndex is long prevEndIndex)
                    {
                        if (prevEndIndex >= next.StartIndex)
                        {
                            throw new ArgumentOutOfRangeException(
                                $"Previous {nameof(SpannedSubPolicy<T>)} overlaps with " +
                                $"next {nameof(SpannedSubPolicy<T>)}");
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Previous {nameof(SpannedSubPolicy<T>)} overlaps with " +
                            $"next {nameof(SpannedSubPolicy<T>)}");
                    }
                }

                prev = next;
            }
        }

        [Pure]
        public T DefaultValue => _defaultValue;

        [Pure]
        public ImmutableList<SpannedSubPolicy<T>> SpannedSubPolicies => _spannedSubPolicies;

        [Pure]
        public bool IsEmpty => SpannedSubPolicies.Count == 0;

        [Pure]
        public Func<long, T> ToGetter()
        {
            T ValueSelector(long index)
            {
                List<SpannedSubPolicy<T>> spannedSubPolicies = SpannedSubPolicies
                    .Where(spannedSubPolicy => spannedSubPolicy.IsTargetIndex(index))
                    .ToList();

                if (spannedSubPolicies.Count > 1)
                {
                    throw new ArgumentException(
                        $"More than one subpolicy is selected where it should not be possible.");
                }
                else if (spannedSubPolicies.Count == 1)
                {
                    return spannedSubPolicies.Single().Value;
                }
                else
                {
                    return DefaultValue;
                }
            }

            return index => ValueSelector(index);
        }

        /// <summary>
        /// Creates a new <see cref="VariableSubPolicy{T}"/> instance with a default behavior.
        /// </summary>
        /// <param name="defaultValue">The default value to use when none of
        /// <see cref="SpannedSubPolicies{T}"/> apply.
        /// </param>
        /// <returns>
        /// A newly created subpolicy with given <paramref name="defaultValue"/> as
        /// its default behavior.
        /// </returns>
        [Pure]
        public static VariableSubPolicy<T> Create(T defaultValue)
        {
            return new VariableSubPolicy<T>(defaultValue);
        }
    }
}
