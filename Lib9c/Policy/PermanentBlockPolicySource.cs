using System.Collections.Immutable;
using Libplanet;

namespace Nekoyume.BlockChain.Policy
{
    public static class PermanentBlockPolicySource
    {
        public const long AuthorizedMinersPolicyInterval = 2;

        public static readonly ImmutableHashSet<Address> AuthorizedMiners = new Address[]
        {
            new Address("82b857D3fE3Bd09d778B40f0a8430B711b3525ED"),
        }.ToImmutableHashSet();

        public static readonly ImmutableHashSet<Address> PermissionedMiners = new Address[]
        {
            new Address("211afcd0E152A61C92600D6a5a63Ca088a85Fbb1"),
            new Address("8a393e376d6Fd3b837314c7d4e249cc90a6B7B17"),
            new Address("590c887BDac8d957Ca5d3c1770489Cf2aFBd868E"),
        }.ToImmutableHashSet();
    }
}
