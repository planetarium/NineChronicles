namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using System.Collections.Immutable;
    using Libplanet.Crypto;
    using Nekoyume.Model.Coupons;

    public static class CouponsFixture
    {
        public static Address AgentAddress1 { get; } =
            new Address("0000000000000000000000000000000000000000");

        public static Address AgentAddress2 { get; } =
            new Address("0000000000000000000000000000000000000001");

        public static Address AgentAddress3 { get; } =
            new Address("0000000000000000000000000000000000000002");

        public static RewardSet RewardSet1 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(302001, 1));

        public static RewardSet RewardSet2 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(302001, 1)
                .Add(302002, 2));

        public static RewardSet RewardSet3 { get; } =
            new RewardSet(ImmutableDictionary<int, uint>.Empty
                .Add(302002, 2));

        public static Guid Guid1 { get; } = new Guid("9CB96C65-3D47-4BAD-8BE6-18D97042B6C9");

        public static Guid Guid2 { get; } = new Guid("85BECFD9-7F5A-4C14-A2E7-C6EA83A23758");

        public static Guid Guid3 { get; } = new Guid("BC911842-11E3-48EB-BFFC-3719465718A5");
    }
}
