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

        public static AuthorizedMinersPolicy Default =>
            new AuthorizedMinersPolicy(ImmutableHashSet<Address>.Empty);

        public static AuthorizedMinersPolicy Mainnet =>
            Default
                .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                    startIndex: 0,
                    endIndex: BlockPolicySource.AuthorizedMinersPolicyEndIndex,
                    predicate: index => index % BlockPolicySource.AuthorizedMinersPolicyInterval == 0,
                    value: BlockPolicySource.AuthorizedMiners));
    }
}
