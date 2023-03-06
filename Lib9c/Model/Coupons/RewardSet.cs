#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Bencodex.Types;

namespace Nekoyume.Model.Coupons
{
    public readonly struct RewardSet : IImmutableDictionary<int, uint>, IEquatable<RewardSet>
    {
        private readonly ImmutableDictionary<int, uint> _rewards;

        private IImmutableDictionary<int, uint> Rewards =>
            _rewards ?? ImmutableDictionary<int, uint>.Empty;

        public RewardSet(in ImmutableDictionary<int, uint> rewards)
        {
            _rewards = rewards;
        }

        public RewardSet(IEnumerable<(int ItemId, uint Quantity)> rewards)
            : this(rewards.ToImmutableDictionary(
                pair => pair.ItemId,
                pair => pair.Quantity
            ))
        {
        }

        public RewardSet(params (int ItemId, uint Quantity)[] rewards)
            : this((IEnumerable<(int ItemId, uint Quantity)>)rewards)
        {
        }

        public RewardSet(Bencodex.Types.Dictionary serialized)
            : this(
#pragma warning disable LAA1002
                serialized.ToImmutableDictionary(
                    pair => int.Parse((Text)pair.Key, CultureInfo.InvariantCulture),
                    pair => (uint)(Bencodex.Types.Integer)pair.Value
                )
#pragma warning restore LAA1002
            )
        {
        }

        public Bencodex.Types.Dictionary Serialize() =>
            new Bencodex.Types.Dictionary(
#pragma warning disable LAA1002
                Rewards.Select(pair =>
                    KeyValuePair.Create<string, IValue>(
                        pair.Key.ToString(CultureInfo.InvariantCulture),
                        (Bencodex.Types.Integer)pair.Value
                    )
                )
#pragma warning restore LAA1002
            );

        public uint this[int key] => Rewards[key];

        public IEnumerable<int> Keys => Rewards.Keys;

        public IEnumerable<uint> Values => Rewards.Values;

        public int Count => Rewards.Count;

        public IImmutableDictionary<int, uint> Add(int key, uint value) =>
            Rewards.Add(key, value);

        public IImmutableDictionary<int, uint> AddRange(IEnumerable<KeyValuePair<int, uint>> pairs) =>
            Rewards.AddRange(pairs);

        public IImmutableDictionary<int, uint> Clear() =>
            Rewards.Clear();

        public bool Contains(KeyValuePair<int, uint> pair) =>
            Rewards.Contains(pair);

        public bool ContainsKey(int key) =>
            Rewards.ContainsKey(key);

        public IEnumerator<KeyValuePair<int, uint>> GetEnumerator() =>
            Rewards.GetEnumerator();

        public IImmutableDictionary<int, uint> Remove(int key) =>
            Rewards.Remove(key);

        public IImmutableDictionary<int, uint> RemoveRange(IEnumerable<int> keys) =>
            Rewards.RemoveRange(keys);

        public IImmutableDictionary<int, uint> SetItem(int key, uint value) =>
            Rewards.SetItem(key, value);

        public IImmutableDictionary<int, uint> SetItems(IEnumerable<KeyValuePair<int, uint>> items) =>
            Rewards.SetItems(items);

        public bool TryGetKey(int equalKey, out int actualKey) =>
            Rewards.TryGetKey(equalKey, out actualKey);

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out uint value) =>
            Rewards.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable)Rewards.OrderBy(pair => pair.Key)).GetEnumerator();

        public bool Equals(RewardSet other) =>
            Rewards.Count == other.Rewards.Count &&
#pragma warning disable LAA1002
            Rewards.All(pair =>
                other.Rewards.TryGetValue(pair.Key, out uint v) && v == pair.Value);
#pragma warning restore LAA1002

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is RewardSet other && Equals(other);

        public override int GetHashCode()
        {
            int hash = Count.GetHashCode();
            foreach (var pair in Rewards.OrderBy(pair => pair.Key))
            {
                unchecked
                {
                    hash = hash * 31 + pair.Key.GetHashCode();
                    hash = hash * 31 + pair.Value.GetHashCode();
                }
            }

            return hash;
        }

        public struct Comparer : IComparer<RewardSet>
        {
            public int Compare(RewardSet x, RewardSet y)
            {
                if (x.Count != y.Count)
                {
                    return x.Count.CompareTo(y.Count);
                }

                foreach (var pair in x.OrderBy(pair => pair.Key))
                {
                    if (!y.TryGetValue(pair.Key, out uint v))
                    {
                        return 1;
                    }

                    if (pair.Value != v)
                    {
                        return pair.Value.CompareTo(v);
                    }
                }

                return 0;
            }
        }
    }
}
