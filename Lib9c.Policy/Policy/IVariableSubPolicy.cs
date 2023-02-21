using System;
using System.Diagnostics.Contracts;
using System.Collections.Immutable;
using Libplanet.Blocks;

namespace Nekoyume.BlockChain.Policy
{
    public interface IVariableSubPolicy<T>
    {
        /// <summary>
        /// Value to use as a fallback if none of the <see cref="SpannedSubPolicy{T}"/> in
        /// <see cref="SpannedSubPolicies"/> apply.
        /// </summary>
        /// <remarks>
        /// <pr>
        /// It is recommended set this property to a value that would always pass the predicate
        /// for validation in case there is a policy gap.
        /// </pr>
        /// <pr>
        /// For instance, for checking the max number of transactions allowed per block, it is
        /// better to set <see cref="DefaultValue"/> as <c>int.MaxValue</c> then overwrite it
        /// with some sensible value such as <c>100</c> by adding a new
        /// <see cref="SpannedSubPolicy{T}"/>.
        /// </pr>
        /// </remarks>
        /// <seealso cref="Getter"/>
        [Pure]
        T DefaultValue { get; }

        /// <summary>
        /// An <see cref="ImmutableList{T}"/> of <see cref="SpannedSubPolicy{T}"/>s that makes
        /// up a <see cref="IVariableSubPolicy{T}"/>.
        /// </summary>
        /// <remarks>
        /// It must be guaranteed that the spans of two different <see cref="SpannedSubPolicy{T}"/>s
        /// do not overlap each other and the spans are in ascending order.
        /// </remarks>
        [Pure]
        ImmutableList<SpannedSubPolicy<T>> SpannedSubPolicies { get; }

        /// <summary>
        /// A mapping from the index of a <see cref="Block{T}"/> to a binding value used for
        /// the policy constraint in consideration.
        /// </summary>
        /// <seealso cref="DefaultValue"/>
        [Pure]
        Func<long, T> Getter { get; }

        /// <summary>
        /// Creates a new subpolicy with an additional <see cref="SpannedSubPolicy{T}"/> added.
        /// </summary>
        /// <param name="spannedSubPolicy">New <see cref="SpannedSubPolicy{T}"/> to add.</param>
        /// <returns>
        /// A new <see cref="IVariableSubPolicy{T}"/> instance with
        /// <paramref name="spannedSubPolicy"/> added at the end.
        /// </returns>
        /// <remarks>
        /// Last spanned subpolicy will be cut short and adjusted accordingly before
        /// adding <paramref name="spannedSubPolicy"/> if <paramref name="spannedSubPolicy"/>
        /// overlaps with the last one.
        /// </remarks>
        IVariableSubPolicy<T> Add(SpannedSubPolicy<T> spannedSubPolicy);

        /// <summary>
        /// Creates a new subpolicy with an additional <see cref="ImmutableList{T}"/> of
        /// <see cref="SpannedSubPolicy{T}"/> added sequentially.
        /// </summary>
        /// <param name="spannedSubPolicies">An <see cref="ImmutableList{T}"/> of
        /// <see cref="SpannedSubPolicy{T}"/>s to add.</param>
        /// <returns>
        /// A new <see cref="IVariableSubPolicy{T}"/> instance with
        /// <paramref name="spannedSubPolicies"/> added at the end.
        /// </returns>
        IVariableSubPolicy<T> AddRange(ImmutableList<SpannedSubPolicy<T>> spannedSubPolicies);

        /// <summary>
        /// Checks if the an instance of <see cref="IVariableSubPolicy{T}"/> applies to
        /// given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of a possibly-yet-to-be-mined <see cref="Block{T}"/>
        /// to check.</param>
        /// <returns><c>true</c> if <paramref name="index"/> is target for any
        /// <see cref="SpannedSubPolicy{T}"/> in <see cref="SpannedSubPolicies"/>.  Otherwise,
        /// <c>fase</c>.</returns>
        /// <remarks>
        /// Call to this method must only be used <em>sparingly</em> and should be <em>avoided</em>
        /// if possible.  Usage of this method indicates dependency coupling between two
        /// different <see cref="IVariableSubPolicy{T}"/>s.
        /// </remarks>
        bool IsTargetIndex(long index);
    }
}
