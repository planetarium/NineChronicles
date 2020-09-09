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
        public readonly Dictionary<Address, ImmutableHashSet<Address>> rankingMap;

        public RankingState() : base(Address)
        {
            rankingMap = new Dictionary<Address, ImmutableHashSet<Address>>();
            for (var i = 0; i < RankingMapCapacity; i++)
            {
                rankingMap[Derive(i)] = new HashSet<Address>().ToImmutableHashSet();
            }
        }

        public RankingState(Dictionary serialized)
            : base(serialized)
        {
            rankingMap = ((Dictionary) serialized["ranking_map"]).ToDictionary(
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
                var value = rankingMap[Derive(i)];
                if (value.Count + 1 <= RankingMapState.Capacity)
                {
                    rankingMap[key] = value.Add(avatarAddress);
                    return key;
                }
            }
            throw new RankingExceededException();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ranking_map"] = new Dictionary(rankingMap.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary)kv.Key.Serialize(),

                        new List(kv.Value.Select(a => a.Serialize()))
                    )
                )),
            }.Union((Dictionary)base.Serialize()));
    }
}
