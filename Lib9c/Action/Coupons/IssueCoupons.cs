using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Coupons;

namespace Nekoyume.Action.Coupons
{
    [Serializable]
    [ActionType("issue_coupons")]
    public sealed class IssueCoupons : GameAction
    {
        public IssueCoupons()
        {
        }

        public IssueCoupons(IImmutableDictionary<RewardSet, uint> rewards, Address recipient)
        {
            Rewards = rewards;
            Recipient = recipient;
        }

        public IImmutableDictionary<RewardSet, uint> Rewards { get; private set; }

        public Address Recipient { get; private set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            if (context.Rehearsal)
            {
                return states.SetCouponWallet(
                    Recipient,
                    ImmutableDictionary.Create<Guid, Coupon>(),
                    rehearsal: true);
            }

            CheckPermission(context);

            var wallet = states.GetCouponWallet(Recipient);
            var random = context.Random;
            var idBytes = new byte[16];
            var orderedRewards = Rewards.OrderBy(kv => kv.Key, default(RewardSet.Comparer));
            foreach (var (rewardSet, quantity) in orderedRewards)
            {
                for (uint i = 0U; i < quantity; i++)
                {
                    random.NextBytes(idBytes);
                    var id = new Guid(idBytes);
                    var coupon = new Coupon(id, rewardSet);
                    wallet = wallet.Add(coupon.Id, coupon);
                }
            }

            return states.SetCouponWallet(Recipient, wallet);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("recipient", new Binary(Recipient.ByteArray))
                .Add(
                    "rewards",
                    new Bencodex.Types.List(
                        Rewards
                            .OrderBy(kv => kv.Key, default(RewardSet.Comparer))
                            .Select(kv => Bencodex.Types.Dictionary.Empty
                                .Add("rewardSet", kv.Key.Serialize())
                                .Add("quantity", kv.Value))
                    )
                );

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Recipient = new Address(plainValue["recipient"]);
            Rewards = ((Bencodex.Types.List)plainValue["rewards"])
                .OfType<Bencodex.Types.Dictionary>()
                .ToImmutableDictionary(
                    d => new RewardSet((Dictionary)d["rewardSet"]),
                    d => (uint)(Integer)d["quantity"]
                );
        }
    }
}
