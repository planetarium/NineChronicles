using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class PermissionedMinersPolicy : VariableSubPolicy<ImmutableHashSet<Address>>
    {
        private PermissionedMinersPolicy(ImmutableHashSet<Address> defaultValue)
            : base(defaultValue)
        {
        }

        private PermissionedMinersPolicy(
            PermissionedMinersPolicy permissionedMinersPolicy,
            SpannedSubPolicy<ImmutableHashSet<Address>> spannedSubPolicy)
            : base(permissionedMinersPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<ImmutableHashSet<Address>> Default =>
            new PermissionedMinersPolicy(ImmutableHashSet<Address>.Empty);

        public static IVariableSubPolicy<ImmutableHashSet<Address>> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                    startIndex: BlockPolicySource.PermissionedMiningStartIndex,
                    endIndex: null,
                    filter: null,
                    value: BlockPolicySource.AuthorizedMiners));

         public static IVariableSubPolicy<ImmutableHashSet<Address>> Permanent =>
             Default
                 .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                     startIndex: 0,
                     endIndex: null,
                     filter: null,
                     value: PermanentBlockPolicySource.PermissionedMiners));
    }
}
