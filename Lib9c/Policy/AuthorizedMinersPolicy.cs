using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class AuthorizedMinersPolicy : VariableSubPolicy<ImmutableHashSet<Address>>
    {
        private AuthorizedMinersPolicy(ImmutableHashSet<Address> defaultValue)
            : base(defaultValue)
        {
        }

        private AuthorizedMinersPolicy(
            AuthorizedMinersPolicy authorizedMinersPolicy,
            SpannedSubPolicy<ImmutableHashSet<Address>> spannedSubPolicy)
            : base(authorizedMinersPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<ImmutableHashSet<Address>> Default =>
            new AuthorizedMinersPolicy(ImmutableHashSet<Address>.Empty);

        public static IVariableSubPolicy<ImmutableHashSet<Address>> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                    startIndex: 0,
                    endIndex: BlockPolicySource.AuthorizedMinersPolicyEndIndex,
                    filter: index => index % BlockPolicySource.AuthorizedMinersPolicyInterval == 0,
                    value: BlockPolicySource.AuthorizedMiners));

        public static IVariableSubPolicy<ImmutableHashSet<Address>> Permanent =>
            Default
                .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                    startIndex: 0,
                    endIndex: null,
                    filter: index => index % PermanentBlockPolicySource.AuthorizedMinersPolicyInterval == 0,
                    value: PermanentBlockPolicySource.AuthorizedMiners));
    }
}
