namespace Lib9c.Tests.Action.Coupons
{
    using System.Collections.Immutable;
    using Libplanet;
    using Nekoyume.Model.Coupons;

    public static class CouponsFixture
    {
        public static Address AgentAddress1 { get; } =
            new Address("0000000000000000000000000000000000000000");

        public static Address AgentAddress2 { get; } =
            new Address("0000000000000000000000000000000000000001");

        public static RewardSet RewardSet1 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(1, 2));

        public static RewardSet RewardSet2 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(1, 2)
                .Add(3, 4));

        public static RewardSet RewardSet3 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(3, 4));
    }
}
