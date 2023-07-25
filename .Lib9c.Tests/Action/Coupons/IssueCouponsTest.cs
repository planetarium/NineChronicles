namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using Nekoyume.Action.Coupons;
    using Nekoyume.Model.Coupons;
    using Nekoyume.Model.State;
    using Xunit;

    public class IssueCouponsTest
    {
        [Fact]
        public void Execute()
        {
            IAccountStateDelta state = new Lib9c.Tests.Action.MockStateDelta()
                .SetState(
                    AdminState.Address,
                    new AdminState(CouponsFixture.AgentAddress1, 1)
                        .Serialize());
            IRandom random = new TestRandom();

            Assert.Throws<PolicyExpiredException>(() =>
                new IssueCoupons(
                        ImmutableDictionary<RewardSet, uint>.Empty,
                        CouponsFixture.AgentAddress1)
                    .Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Rehearsal = false,
                            Random = random,
                            BlockIndex = long.MaxValue,
                            Signer = CouponsFixture.AgentAddress1,
                        }));

            Assert.Throws<PermissionDeniedException>(() =>
                new IssueCoupons(
                        ImmutableDictionary<RewardSet, uint>.Empty,
                        CouponsFixture.AgentAddress1)
                    .Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Rehearsal = false,
                            Random = random,
                            BlockIndex = 0,
                            Signer = CouponsFixture.AgentAddress2,
                        }));

            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty,
                new IssueCoupons(
                    ImmutableDictionary<RewardSet, uint>.Empty,
                    CouponsFixture.AgentAddress1)
                    .Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Rehearsal = false,
                            Random = random,
                            BlockIndex = 0,
                            Signer = CouponsFixture.AgentAddress1,
                        })
                    .GetCouponWallet(CouponsFixture.AgentAddress1));

            Assert.Equal(
                Bencodex.Types.Null.Value,
                new IssueCoupons(
                        ImmutableDictionary<RewardSet, uint>.Empty
                            .Add(CouponsFixture.RewardSet1, 1)
                            .Add(CouponsFixture.RewardSet2, 2),
                        CouponsFixture.AgentAddress1)
                    .Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Rehearsal = true,
                            Random = random,
                            BlockIndex = 0,
                            Signer = CouponsFixture.AgentAddress1,
                        })
                    .GetState(CouponsFixture.AgentAddress1.Derive(SerializeKeys.CouponWalletKey)));

            state = new IssueCoupons(
                    ImmutableDictionary<RewardSet, uint>.Empty
                        .Add(CouponsFixture.RewardSet1, 1)
                        .Add(CouponsFixture.RewardSet2, 2),
                    CouponsFixture.AgentAddress1)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Rehearsal = false,
                        Random = random,
                        BlockIndex = 0,
                        Signer = CouponsFixture.AgentAddress1,
                    });

            state = new IssueCoupons(
                    ImmutableDictionary<RewardSet, uint>.Empty
                        .Add(CouponsFixture.RewardSet3, 3),
                    CouponsFixture.AgentAddress2)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Rehearsal = false,
                        Random = random,
                        BlockIndex = 0,
                        Signer = CouponsFixture.AgentAddress1,
                    });

            var agent1CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress1);
            var agent2CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress2);

            Assert.Equal(3, agent1CouponWallet.Count);
            Assert.Equal(1, agent1CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet1)));
            Assert.Equal(2, agent1CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet2)));
            Assert.Equal(0, agent1CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet3)));

            Assert.Equal(3, agent1CouponWallet.Count);
            Assert.Equal(0, agent2CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet1)));
            Assert.Equal(0, agent2CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet2)));
            Assert.Equal(3, agent2CouponWallet.Count(
                item => item.Value.Rewards.Equals(CouponsFixture.RewardSet3)));
        }

        [Fact]
        public void PlainValue()
        {
            var action = new IssueCoupons(
                ImmutableDictionary<RewardSet, uint>.Empty
                    .Add(CouponsFixture.RewardSet2, 1)
                    .Add(CouponsFixture.RewardSet1, 2),
                CouponsFixture.AgentAddress1);

            Assert.Equal(
                new Bencodex.Types.Dictionary(
                    ImmutableDictionary<string, IValue>.Empty
                        .Add("recipient", new Binary(CouponsFixture.AgentAddress1.ByteArray))
                        .Add(
                            "rewards",
                            Bencodex.Types.List.Empty
                                .Add(Bencodex.Types.Dictionary.Empty
                                    .Add("rewardSet", CouponsFixture.RewardSet1.Serialize())
                                    .Add("quantity", 2))
                                .Add(Bencodex.Types.Dictionary.Empty
                                    .Add("rewardSet", CouponsFixture.RewardSet2.Serialize())
                                    .Add("quantity", 1)))
                        .Select(kv => new KeyValuePair<IKey, IValue>((Text)kv.Key, kv.Value))),
                ((Dictionary)((Dictionary)action.PlainValue)["values"]).Remove((Text)"id"));
        }

        [Fact]
        public void LoadPlainValue()
        {
            var expected = new IssueCoupons(
                ImmutableDictionary<RewardSet, uint>.Empty
                    .Add(CouponsFixture.RewardSet2, 1)
                    .Add(CouponsFixture.RewardSet1, 2),
                CouponsFixture.AgentAddress1);
            var actual = new IssueCoupons(
                ImmutableDictionary<RewardSet, uint>.Empty,
                CouponsFixture.AgentAddress1);
            actual.LoadPlainValue(
                Dictionary.Empty
                    .Add("type_id", "issue_coupons")
                    .Add("values", new Bencodex.Types.Dictionary(
                        ImmutableDictionary<string, IValue>.Empty
                            .Add("recipient", new Binary(CouponsFixture.AgentAddress1.ByteArray))
                            .Add(
                                "rewards",
                                Bencodex.Types.List.Empty
                                    .Add(Bencodex.Types.Dictionary.Empty
                                        .Add("rewardSet", CouponsFixture.RewardSet1.Serialize())
                                        .Add("quantity", 2))
                                    .Add(Bencodex.Types.Dictionary.Empty
                                        .Add("rewardSet", CouponsFixture.RewardSet2.Serialize())
                                        .Add("quantity", 1)))
                            .Select(kv => new KeyValuePair<IKey, IValue>((Text)kv.Key, kv.Value)))
                    .SetItem("id", new Guid("6E69DC55-A0D0-435A-A787-C62356CBE517").Serialize())));

            Assert.Equal(expected.Rewards, actual.Rewards);
            Assert.Equal(expected.Recipient, actual.Recipient);
        }
    }
}
