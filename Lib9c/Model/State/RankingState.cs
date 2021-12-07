using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class RankingState : State
    {
        public static readonly Address Address = Addresses.Ranking;
        public const int RankingMapCapacity = 100;
        public readonly Dictionary<Address, ImmutableHashSet<Address>> RankingMap;

        public RankingState() : base(Address)
        {
            RankingMap = new Dictionary<Address, ImmutableHashSet<Address>>();
            for (var i = 0; i < RankingMapCapacity; i++)
            {
                RankingMap[Derive(i)] = new HashSet<Address>().ToImmutableHashSet();
            }
        }

        public RankingState(Dictionary serialized)
            : base(serialized)
        {
            RankingMap = ((Dictionary) serialized["ranking_map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => kv.Value.ToList(StateExtensions.ToAddress).ToImmutableHashSet()
            );
        }

        public static Address Derive(int index)
        {
            return Address.Derive($"ranking_{index}");
        }

        public Address UpdateRankingMap(Address avatarAddress)
        {
            for (var i = 0; i < RankingMapCapacity; i++)
            {
                var key = Derive(i);
                var value = RankingMap[Derive(i)];
                if (value.Count + 1 <= RankingMapState.Capacity)
                {
                    RankingMap[key] = value.Add(avatarAddress);
                    return key;
                }
            }
            throw new RankingExceededException();
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            var rankingMapValue = new Bencodex.Types.Dictionary(RankingMap.Select(pair =>
#pragma warning restore LAA1002
                new KeyValuePair<IKey, IValue>(
                    (Bencodex.Types.Binary)pair.Key.Serialize(),
                    new Bencodex.Types.List(pair.Value
                        .OrderBy(e => e.GetHashCode())
                        .Select(e => e.Serialize())))));
            return ((Bencodex.Types.Dictionary)base.Serialize())
                .SetItem("ranking_map", rankingMapValue);
        }

        [Obsolete("Since it contains a deterministic problem, replace it with the `Serialize()` method that operates deterministically like this logic.")]
        public IValue SerializeV1() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ranking_map"] = new Dictionary(RankingMap.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary)kv.Key.Serialize(),

                        new List(kv.Value.Select(a => a.Serialize()))
                    )
                )),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
    }
}
