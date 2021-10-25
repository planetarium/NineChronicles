using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Libplanet.Blocks;

namespace Nekoyume.BlockChain.Policy
{
    public abstract class VariableSubPolicy<T> : IVariableSubPolicy<T>
    {
        /// <inheritdoc/>
        [Pure]
        public T DefaultValue { get; private set; }

        /// <inheritdoc/>
        [Pure]
        public ImmutableList<SpannedSubPolicy<T>> SpannedSubPolicies { get; private set; }

        /// <inheritdoc/>
        [Pure]
        public Func<long, T> Getter { get; private set; }

        protected VariableSubPolicy(T defaultValue)
        {
            DefaultValue = defaultValue;
            SpannedSubPolicies = ImmutableList<SpannedSubPolicy<T>>.Empty;
            Getter = ToGetter();

            Validate();
        }

        protected VariableSubPolicy(
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
                SpannedSubPolicy<T> lastSpannedSubPolicy =
                    spannedSubPolicies[spannedSubPolicies.Count - 1];

                // If spannedSubPolicies.StartIndex <= lastSpannedSubPolicy.StartIndex
                // an exception will be automatically thrown when trying to create
                // a new SpannedSubPolicy<T> below.
                if (!(lastSpannedSubPolicy.EndIndex is long endIndex) ||
                    endIndex >= spannedSubPolicy.StartIndex)
                {
                    lastSpannedSubPolicy = new SpannedSubPolicy<T>(
                        lastSpannedSubPolicy.StartIndex,
                        spannedSubPolicy.StartIndex - 1,
                        lastSpannedSubPolicy.Filter,
                        lastSpannedSubPolicy.Value);
                    spannedSubPolicies[spannedSubPolicies.Count - 1] = lastSpannedSubPolicy;
                }
            }

            spannedSubPolicies.Add(spannedSubPolicy);

            DefaultValue = variableSubPolicy.DefaultValue;
            SpannedSubPolicies = spannedSubPolicies.ToImmutableList();
            Getter = ToGetter();

            Validate();
        }

        /// <inheritdoc/>
        [Pure]
        public IVariableSubPolicy<T> Add(SpannedSubPolicy<T> spannedSubPolicy)
        {
            try
            {
                return (VariableSubPolicy<T>)Activator.CreateInstance(
                        this.GetType(),
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new object[] { this, spannedSubPolicy },
                        null);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is ArgumentOutOfRangeException aoore)
                {
                    throw aoore;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        [Pure]
        public IVariableSubPolicy<T> AddRange(
            ImmutableList<SpannedSubPolicy<T>> spannedSubPolicies)
        {
            IVariableSubPolicy<T> variableSubPolicy = this;
            foreach (SpannedSubPolicy<T> spannedSubPolicy in spannedSubPolicies)
            {
                variableSubPolicy = variableSubPolicy.Add(spannedSubPolicy);
            }
            return variableSubPolicy;
        }

        /// <inheritdoc/>
        [Pure]
        public bool IsTargetIndex(long index) =>
            SpannedSubPolicies.Any(spannedSubPolicy => spannedSubPolicy.IsTargetIndex(index));

        [Pure]
        private Func<long, T> ToGetter()
        {
            return index => SpannedSubPolicies
                .FirstOrDefault(spannedSubPolicy => spannedSubPolicy.IsTargetIndex(index)) is SpannedSubPolicy<T> ssp
                    ? ssp.Value
                    : DefaultValue;
        }

        /// <summary>
        /// Checks if a <see cref="VariableSubPolicy{T}"/> instance has sound data.
        /// Should be called inside every constructor at the end.
        /// </summary>
        /// <remarks>
        /// This only checks if any pair of <see cref="SpannedSubPolicy{T}"/>s overlap with
        /// each other to ensure that there is no ambiguity on selecting the binding argument
        /// when <see cref="Getter"/> is called with an index.
        /// </remarks>
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
                                paramName: nameof(prevEndIndex),
                                actualValue: prevEndIndex,
                                message: $"Previous {nameof(SpannedSubPolicy<T>)} overlaps with " +
                                    $"next {nameof(SpannedSubPolicy<T>)}");
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(
                            $"Previous {nameof(SpannedSubPolicy<T>)} overlaps with " +
                            $"next {nameof(SpannedSubPolicy<T>)}.");
                    }
                }

                prev = next;
            }
        }
    }
}
