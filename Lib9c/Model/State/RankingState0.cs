using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class RankingState0 : State, ISerializable
    {
        private static readonly Codec _codec = new Codec();
        public static readonly Address Address = Addresses.Ranking;
        public const int RankingMapCapacity = 100;
        private Dictionary<Address, ImmutableHashSet<Address>> _rankingMap;
        private Dictionary _serialized;

        public RankingState0() : base(Address)
        {
            _rankingMap = new Dictionary<Address, ImmutableHashSet<Address>>();
            for (var i = 0; i < RankingMapCapacity; i++)
            {
                _rankingMap[Derive(i)] = ImmutableHashSet<Address>.Empty;
            }
        }

        public RankingState0(Dictionary serialized)
            : base(serialized)
        {
            _serialized = serialized;
        }

        public RankingState0(SerializationInfo info, StreamingContext context)
            : this((Dictionary)_codec.Decode(
                (byte[])info.GetValue(nameof(_serialized), typeof(byte[]))
            ))
        {
        }

        public Dictionary<Address, ImmutableHashSet<Address>> RankingMap
        {
            get
            {
                if (_rankingMap is null)
                {
                    _rankingMap = _serialized.GetValue<Dictionary>("ranking_map").ToDictionary(
                        kv => kv.Key.ToAddress(),
                        kv => kv.Value.ToImmutableHashSet(StateExtensions.ToAddress)
                    );
                    _serialized = null;
                }

                return _rankingMap;
            }
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

        public override IValue Serialize() => _serialized ??
            ((Dictionary)base.Serialize()).Add(
                "ranking_map",
#pragma warning disable LAA1002
                new Dictionary(RankingMap.Select(kv =>
#pragma warning restore LAA1002
                    new KeyValuePair<IKey, IValue>(
                        (IKey)kv.Key.Serialize(),
                        new List(kv.Value
                            .OrderBy(v => v.GetHashCode())
                            .Select(StateExtensions.Serialize))
                    )
                ))
            );

        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            info.AddValue(nameof(_serialized), _codec.Encode(Serialize()));
    }
}
