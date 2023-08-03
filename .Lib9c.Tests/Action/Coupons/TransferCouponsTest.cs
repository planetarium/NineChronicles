namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Coupons;
    using Nekoyume.Model.Coupons;
    using Nekoyume.Model.State;
    using Xunit;

    public class TransferCouponsTest
    {
        [Fact]
        public void Execute()
        {
            IAccountStateDelta state = new Lib9c.Tests.Action.MockStateDelta();
            IRandom random = new TestRandom();

            var coupon1 = new Coupon(CouponsFixture.Guid1, CouponsFixture.RewardSet1);
            var coupon2 = new Coupon(CouponsFixture.Guid2, CouponsFixture.RewardSet2);
            var coupon3 = new Coupon(CouponsFixture.Guid3, CouponsFixture.RewardSet3);

            state = state
                .SetCouponWallet(
                    CouponsFixture.AgentAddress1,
                    state.GetCouponWallet(CouponsFixture.AgentAddress1)
                        .Add(CouponsFixture.Guid1, coupon1)
                        .Add(CouponsFixture.Guid2, coupon2)
                        .Add(CouponsFixture.Guid3, coupon3));

            // can't transfer a nonexistent coupon
            Assert.Throws<FailedLoadStateException>(() => new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty.Add(
                            new Guid("97529656-CB7F-45C6-8466-A072DD2DBFBD"))))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    }));

            // can't transfer coupon that's not mine
            Assert.Throws<FailedLoadStateException>(() => new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress1, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress2,
                        Rehearsal = false,
                    }));

            // can't transfer a coupon to two different people
            Assert.Throws<FailedLoadStateException>(() => new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1))
                        .Add(CouponsFixture.AgentAddress3, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    }));

            // transfer to self
            var expected = state.GetCouponWallet(CouponsFixture.AgentAddress1);
            state = new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress1, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    });
            Assert.Equal(expected, state.GetCouponWallet(CouponsFixture.AgentAddress1));

            // transfer nothing
            state = new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    });
            Assert.Equal(expected, state.GetCouponWallet(CouponsFixture.AgentAddress1));

            // single transfer
            expected = state.GetCouponWallet(CouponsFixture.AgentAddress1);
            state = new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    });
            Assert.Equal(
                expected.Remove(CouponsFixture.Guid1),
                state.GetCouponWallet(CouponsFixture.AgentAddress1));
            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(CouponsFixture.Guid1, coupon1),
                state.GetCouponWallet(CouponsFixture.AgentAddress2));

            // can't transfer a coupon twice
            Assert.Throws<FailedLoadStateException>(() => new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    }));

            state = state
                .SetCouponWallet(
                    CouponsFixture.AgentAddress1,
                    ImmutableDictionary<Guid, Coupon>.Empty
                        .Add(CouponsFixture.Guid1, coupon1)
                        .Add(CouponsFixture.Guid2, coupon2)
                        .Add(CouponsFixture.Guid3, coupon3))
                .SetCouponWallet(
                    CouponsFixture.AgentAddress2,
                    ImmutableDictionary<Guid, Coupon>.Empty)
                .SetCouponWallet(
                    CouponsFixture.AgentAddress3,
                    ImmutableDictionary<Guid, Coupon>.Empty);

            var rehearsedState = new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)
                            .Add(CouponsFixture.Guid2))
                        .Add(CouponsFixture.AgentAddress3, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid3)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = true,
                    });
            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    CouponsFixture.AgentAddress1.Derive(SerializeKeys.CouponWalletKey)));
            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    CouponsFixture.AgentAddress2.Derive(SerializeKeys.CouponWalletKey)));
            Assert.Equal(
                ActionBase.MarkChanged,
                rehearsedState.GetState(
                    CouponsFixture.AgentAddress3.Derive(SerializeKeys.CouponWalletKey)));

            // multiple transfer
            state = new TransferCoupons(
                    ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                        .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid1)
                            .Add(CouponsFixture.Guid2))
                        .Add(CouponsFixture.AgentAddress3, ImmutableHashSet<Guid>.Empty
                            .Add(CouponsFixture.Guid3)))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        Rehearsal = false,
                    });
            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty,
                state.GetCouponWallet(CouponsFixture.AgentAddress1));
            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(CouponsFixture.Guid1, coupon1)
                    .Add(CouponsFixture.Guid2, coupon2),
                state.GetCouponWallet(CouponsFixture.AgentAddress2));
            Assert.Equal(
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(CouponsFixture.Guid3, coupon3),
                state.GetCouponWallet(CouponsFixture.AgentAddress3));
        }

        [Fact]
        public void PlainValue()
        {
            var action = new TransferCoupons(
                ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                    .Add(CouponsFixture.AgentAddress1, ImmutableHashSet<Guid>.Empty
                        .Add(CouponsFixture.Guid1))
                    .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                        .Add(CouponsFixture.Guid3)
                        .Add(CouponsFixture.Guid2))
                    .Add(CouponsFixture.AgentAddress3, ImmutableHashSet<Guid>.Empty));

            var expected = new Bencodex.Types.Dictionary(
                ImmutableDictionary<string, IValue>.Empty
                    .Add(
                        "couponsPerRecipient",
                        Bencodex.Types.Dictionary.Empty
                            .Add(
                                (Bencodex.Types.Binary)CouponsFixture.AgentAddress1.ByteArray,
                                Bencodex.Types.List.Empty
                                    .Add(CouponsFixture.Guid1.ToByteArray()))
                            .Add(
                                (Bencodex.Types.Binary)CouponsFixture.AgentAddress2.ByteArray,
                                Bencodex.Types.List.Empty
                                    .Add(CouponsFixture.Guid2.ToByteArray())
                                    .Add(CouponsFixture.Guid3.ToByteArray()))
                            .Add(
                                (Bencodex.Types.Binary)CouponsFixture.AgentAddress3.ByteArray,
                                Bencodex.Types.List.Empty)));

            Assert.Equal(
                expected,
                ((Dictionary)((Dictionary)action.PlainValue)["values"]).Remove((Text)"id"));
        }

        [Fact]
        public void LoadPlainValue()
        {
            var expected = new TransferCoupons(
                ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty
                    .Add(CouponsFixture.AgentAddress1, ImmutableHashSet<Guid>.Empty
                        .Add(CouponsFixture.Guid1))
                    .Add(CouponsFixture.AgentAddress2, ImmutableHashSet<Guid>.Empty
                        .Add(CouponsFixture.Guid3)
                        .Add(CouponsFixture.Guid2))
                    .Add(CouponsFixture.AgentAddress3, ImmutableHashSet<Guid>.Empty));

            var actual =
                new TransferCoupons(ImmutableDictionary<Address, IImmutableSet<Guid>>.Empty);

            actual.LoadPlainValue(Dictionary.Empty
                .Add("type_id", "transfer_coupons")
                .Add("values", new Bencodex.Types.Dictionary(
                    ImmutableDictionary<string, IValue>.Empty
                        .Add(
                            "couponsPerRecipient",
                            Bencodex.Types.Dictionary.Empty
                                .Add(
                                    (Bencodex.Types.Binary)CouponsFixture.AgentAddress1.ByteArray,
                                    Bencodex.Types.List.Empty
                                .Add(CouponsFixture.Guid1.ToByteArray()))
                                .Add(
                                    (Bencodex.Types.Binary)CouponsFixture.AgentAddress2.ByteArray,
                                    Bencodex.Types.List.Empty
                                        .Add(CouponsFixture.Guid2.ToByteArray())
                                        .Add(CouponsFixture.Guid3.ToByteArray()))
                                .Add(
                                    (Bencodex.Types.Binary)CouponsFixture.AgentAddress3.ByteArray,
                                    Bencodex.Types.List.Empty)))
                    .SetItem("id", new Guid("AE3FA099-B97C-480F-9E3A-4E1FEF1EA783").Serialize())));
            Assert.Equal(expected.CouponsPerRecipient, actual.CouponsPerRecipient);
        }
    }
}
