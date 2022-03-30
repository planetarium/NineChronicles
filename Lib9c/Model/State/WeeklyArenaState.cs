using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.TableData;
using LazyArenaInfo =
    Nekoyume.Model.State.LazyState<Nekoyume.Model.State.ArenaInfo, Bencodex.Types.Dictionary>;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class WeeklyArenaState : State, IDictionary<Address, ArenaInfo>, ISerializable
    {
        #region static

        private static Address _baseAddress = Addresses.WeeklyArena;

        public static Address DeriveAddress(int index)
        {
            return _baseAddress.Derive($"weekly_arena_{index}");
        }

        #endregion

        public long ResetIndex;

        public bool Ended;

        private readonly Dictionary<Address, LazyArenaInfo> _map;

        public IReadOnlyDictionary<Address, LazyArenaInfo> Map => _map;

        public List<ArenaInfo> OrderedArenaInfos { get; private set; }

        public WeeklyArenaState(int index) : base(DeriveAddress(index))
        {
            _map = new Dictionary<Address, LazyArenaInfo>();
            ResetOrderedArenaInfos();
        }

        public WeeklyArenaState(Address address) : base(address)
        {
            _map = new Dictionary<Address, LazyArenaInfo>();
            ResetOrderedArenaInfos();
        }

        public WeeklyArenaState(Dictionary serialized) : base(serialized)
        {
            _map = ((Dictionary)serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new LazyArenaInfo((Dictionary)kv.Value, DeserializeArenaInfo)
            );

            ResetIndex = serialized.GetLong("resetIndex");
            Ended = serialized["ended"].ToBoolean();
            ResetOrderedArenaInfos();
        }

        public WeeklyArenaState(IValue iValue) : this((Dictionary)iValue)
        {
        }

        protected WeeklyArenaState(SerializationInfo info, StreamingContext context)
            : this((Dictionary)new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("resetIndex", ResetIndex.Serialize())
            .Add("ended", Ended.Serialize())
#pragma warning disable LAA1002
            .Add("map", new Dictionary(_map.Select(kv =>
#pragma warning restore LAA1002
                new KeyValuePair<IKey, IValue>((IKey)kv.Key.Serialize(), kv.Value.Serialize()))));

        private void ResetOrderedArenaInfos()
        {
            OrderedArenaInfos = _map.Values
                .Select(lazy => lazy.State)
                .OrderByDescending(info => info.Score)
                .ThenBy(info => info.CombatPoint)
                .ToList();
        }

        /// <summary>
        /// Get arena rank information.
        /// </summary>
        /// <param name="firstRank">The first rank in the range that want to get.</param>
        /// <param name="count">The count of the range that want to get.</param>
        /// <returns>A list of tuples that contains <c>int</c> and <c>ArenaInfo</c>.</returns>
        public List<(int rank, ArenaInfo arenaInfo)> GetArenaInfos(
            int firstRank = 1,
            int? count = null)
        {
            if (OrderedArenaInfos.Count == 0)
            {
                return new List<(int rank, ArenaInfo arenaInfo)>();
            }

            if (!(0 < firstRank && firstRank <= OrderedArenaInfos.Count))
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(firstRank)}({firstRank}) out of range({OrderedArenaInfos.Count})");
            }

            count = count.HasValue
                ? Math.Min(OrderedArenaInfos.Count - firstRank + 1, count.Value)
                : OrderedArenaInfos.Count - firstRank + 1;

            var offsetIndex = 0;
            return OrderedArenaInfos.GetRange(firstRank - 1, count.Value)
                .Select(arenaInfo => (firstRank + offsetIndex++, arenaInfo))
                .ToList();
        }

        /// <summary>
        /// Get arena rank information.
        /// </summary>
        /// <param name="avatarAddress">The base value of the range that want to get.</param>
        /// <param name="upperRange">The upper range than base value in the ranges that want to get.</param>
        /// <param name="lowerRange">The lower range than base value in the ranges that want to get.</param>
        /// <returns>A list of tuples that contains <c>int</c> and <c>ArenaInfo</c>.</returns>
        public List<(int rank, ArenaInfo arenaInfo)> GetArenaInfos(
            Address avatarAddress,
            int upperRange = 10,
            int lowerRange = 10)
        {
            var avatarRank = 0;
            for (var i = 0; i < OrderedArenaInfos.Count; i++)
            {
                var pair = OrderedArenaInfos[i];
                if (!pair.AvatarAddress.Equals(avatarAddress))
                {
                    continue;
                }

                avatarRank = i + 1;
                break;
            }

            if (avatarRank == 0)
            {
                return new List<(int rank, ArenaInfo arenaInfo)>();
            }

            var firstRank = Math.Max(1, avatarRank - upperRange);
            var lastRank = Math.Min(avatarRank + lowerRange, OrderedArenaInfos.Count);
            return GetArenaInfos(firstRank, lastRank - firstRank + 1);
        }

        public ArenaInfo GetArenaInfo(Address avatarAddress)
        {
            return OrderedArenaInfos.FirstOrDefault(info => info.AvatarAddress.Equals(avatarAddress));
        }

        private void Update(AvatarState avatarState, CharacterSheet characterSheet, bool active = false)
        {
            Add(avatarState.address, new ArenaInfo(avatarState, characterSheet, active));
        }

        private void UpdateV2(AvatarState avatarState, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet,
            bool active = false)
        {
            Add(avatarState.address, new ArenaInfo(avatarState, characterSheet, costumeStatSheet, active));
        }

        public void Update(ArenaInfo info)
        {
            Add(info.AvatarAddress, info);
        }

        public void Set(AvatarState avatarState, CharacterSheet characterSheet)
        {
            Update(avatarState, characterSheet);
        }

        public void SetV2(AvatarState avatarState, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet)
        {
            UpdateV2(avatarState, characterSheet, costumeStatSheet);
        }

        public void ResetCount(long ctxBlockIndex)
        {
            foreach (var info in _map.Values)
            {
                info.State.ResetCount();
            }

            ResetIndex = ctxBlockIndex;
        }

        public void End()
        {
            Ended = true;
        }

        public void Update(WeeklyArenaState prevState, long index)
        {
#pragma warning disable LAA1002
            foreach (var kv in prevState._map)
#pragma warning restore LAA1002
            {
                LazyArenaInfo lazyArenaInfo = kv.Value;
                bool active =
                    lazyArenaInfo.GetStateOrSerializedEncoding(out ArenaInfo i, out Dictionary d)
                        ? i.Active
                        : ArenaInfo.IsActive(d);
                if (active)
                {
                    // Note that this conditional is necessary and must not be removed.  There had
                    // been a bug where the ArenaInfo.Active & ArenaInfo.Score hadn't been reset
                    // when ArenaInfo updated from v100090 to v100092 (inclusive; fixed in v100093).
                    // Although such behaviour was an unintended bug, anyway there still exist
                    // actions in the 9c-main chain which are immutable and thus must be maintained
                    // for backward compatibility.
                    // Contexts:
                    //   https://canary.discord.com/channels/539405872346955788/613670425729171456/920464205319123024
                    //   https://canary.discord.com/channels/539405872346955788/613670425729171456/920494074476257310
                    //   https://github.com/planetarium/lib9c/blob/v100090/Lib9c/Model/State/WeeklyArenaState.cs#L200-L218
                    //   https://github.com/planetarium/lib9c/blob/v100092/Lib9c/Model/State/ArenaInfo.cs#L99-L111
                    //   (planetarium internal) https://planetariumhq.slack.com/archives/C01D7N32V55/p1639524157031900
                    //   (planetarium internal) https://www.notion.so/planetarium/2021-12-15-1-470a0216e2b44495974e68bab8f51d2d
                    const long bugExistedSince = 2_968_000L;  // Bug-appeared block index
                    const long bugExistedUntil = 3_024_000L;  // Bug-removed block index
                    if (index < bugExistedSince || index >= bugExistedUntil)
                    {
                        // Intended behavior:
                        var prevArenaInfo = lazyArenaInfo.State;
                        var newArenaInfo = new ArenaInfo(prevArenaInfo);  // Reset .Score & .Active
                        _map[kv.Key] = new LazyArenaInfo(newArenaInfo);
                    }
                    else
                    {
                        // Unintended behaviour, which is maintained only for certain blocks:
                        _map[kv.Key] = lazyArenaInfo;
                    }
                }
            }

            ResetIndex = index;
        }

        public void SetReceive(Address avatarAddress)
        {
            _map[avatarAddress].State.Receive = true;
        }

        #region IDictionary

        public IEnumerator<KeyValuePair<Address, ArenaInfo>> GetEnumerator() => _map
            .OrderBy(kv => kv.Key.GetHashCode())
            .Select(LazyArenaInfo.LoadStatePair)
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Address, ArenaInfo> item) =>
            Add(item.Key, item.Value);

        public void Clear()
        {
            _map.Clear();
            ResetOrderedArenaInfos();
        }

        public bool Contains(KeyValuePair<Address, ArenaInfo> item) =>
            _map.TryGetValue(item.Key, out LazyArenaInfo lazy) && lazy.State.Equals(item.Value);

        public void CopyTo(KeyValuePair<Address, ArenaInfo>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Address, ArenaInfo> item)
        {
            return Remove(item.Key);
        }

        public int Count => _map.Count;
        public bool IsReadOnly => false;

        public void Add(Address key, ArenaInfo value)
        {
            _map[key] = new LazyArenaInfo(value);
            ResetOrderedArenaInfos();
        }

        public bool ContainsKey(Address key)
        {
            return _map.ContainsKey(key);
        }

        public bool Remove(Address key)
        {
            var result = _map.Remove(key);
            ResetOrderedArenaInfos();
            return result;
        }

        public bool TryGetValue(Address key, out ArenaInfo value)
        {
            if (_map.TryGetValue(key, out LazyArenaInfo lazy))
            {
                value = lazy.State;
                return true;
            }

            value = null;
            return false;
        }

        public ArenaInfo this[Address key]
        {
            get => _map[key].State;
            set
            {
                _map[key] = new LazyArenaInfo(value);
                ResetOrderedArenaInfos();
            }
        }

        public ICollection<Address> Keys =>
#pragma warning disable S2365
            _map.Keys.OrderBy(k => k.GetHashCode()).ToArray();
#pragma warning restore S2365

        public ICollection<ArenaInfo> Values =>
#pragma warning disable S2365
            _map.OrderBy(kv => kv.Key.GetHashCode()).Select(kv => kv.Value.State).ToArray();
#pragma warning restore S2365

        #endregion

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }

        private static ArenaInfo DeserializeArenaInfo(Dictionary serialized) =>
            new ArenaInfo(serialized);
    }
}
