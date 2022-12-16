#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;

namespace Nekoyume.Model.Coupons
{
    public readonly struct Coupon
        : IOrderedEnumerable<(int ItemId, uint Quantity)>, IEquatable<Coupon>
    {
        public readonly Guid Id;

        public readonly RewardSet Rewards;

        public Coupon(Guid id, params (int ItemId, uint Quantity)[] rewards)
            : this(id, (IEnumerable<(int ItemId, uint Quantity)>)rewards)
        {
        }

        public Coupon(Guid id, IEnumerable<(int ItemId, uint Quantity)> rewards)
            : this(id, new RewardSet(rewards))
        {
        }

        public Coupon(Guid id, in RewardSet rewards)
        {
            Id = id;
            Rewards = rewards;
        }

        public Coupon(IValue serialized)
        {
            if (!(serialized is Bencodex.Types.Dictionary dict))
            {
                throw new ArgumentException(
                    $"Expected {nameof(Bencodex.Types.Dictionary)} but {serialized} was given.",
                    nameof(serialized)
                );
            }

            Id = new Guid(dict.GetValue<Binary>("id"));
            Rewards = new RewardSet((Bencodex.Types.Dictionary)dict["rewards"]);
        }

        public IValue Serialize() => Bencodex.Types.Dictionary.Empty
            .Add("id", Id.ToByteArray())
            .Add("rewards", Rewards.Serialize());

        public override bool Equals(object? obj) => obj is Coupon { } o && o.Equals(this);

        public override int GetHashCode() =>
            HashCode.Combine(Id, Rewards);

        public bool Equals(Coupon other) => Id == other.Id && Rewards.Equals(other.Rewards);

        IOrderedEnumerable<(int ItemId, uint Quantity)> IOrderedEnumerable<(int ItemId, uint Quantity)>.CreateOrderedEnumerable<TKey>(
            Func<(int ItemId, uint Quantity), TKey> keySelector,
            IComparer<TKey>? comparer,
            bool descending)
        {
#pragma warning disable LAA1002
            var pairs = Rewards.Select(pair => (pair.Key, pair.Value));
#pragma warning restore LAA1002
            return descending
                ? pairs.OrderByDescending(keySelector, comparer)
                : pairs.OrderBy(keySelector, comparer);
        }

        IEnumerator<(int ItemId, uint Quantity)> IEnumerable<(int ItemId, uint Quantity)>.GetEnumerator() =>
            this.Rewards.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            this.Rewards.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).GetEnumerator();
    }
}
