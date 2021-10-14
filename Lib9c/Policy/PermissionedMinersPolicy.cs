using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public static class PermissionedMinersPolicy
    {
        public static readonly ImmutableHashSet<Address> DefaultValue = ImmutableHashSet<Address>.Empty;

        public static VariableSubPolicy<ImmutableHashSet<Address>> Default
        {
            get
            {
                return VariableSubPolicy<ImmutableHashSet<Address>>
                    .Create(DefaultValue);
            }
        }

        public static VariableSubPolicy<ImmutableHashSet<Address>> Mainnet
        {
            get
            {
                return Default
                    .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                        startIndex: BlockPolicySource.PermissionedMiningStartIndex,
                        endIndex: null,
                        predicate: null,
                        value: BlockPolicySource.AuthorizedMiners));
            }
        }
    }
}
