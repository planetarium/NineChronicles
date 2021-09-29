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

        private readonly Dictionary<Address, ArenaInfo> _map;

        public List<ArenaInfo> OrderedArenaInfos { get; private set; }

        public WeeklyArenaState(int index) : base(DeriveAddress(index))
        {
            _map = new Dictionary<Address, ArenaInfo>();
            ResetOrderedArenaInfos();
        }

        public WeeklyArenaState(Address address) : base(address)
        {
            _map = new Dictionary<Address, ArenaInfo>();
            ResetOrderedArenaInfos();
        }

        public WeeklyArenaState(Dictionary serialized) : base(serialized)
        {
            _map = ((Dictionary)serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new ArenaInfo((Dictionary)kv.Value)
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

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"map"] = new Dictionary(_map.Select(kv =>
                   new KeyValuePair<IKey, IValue>(
                       (Binary)kv.Key.Serialize(),
                       kv.Value.Serialize()
                   )
                )),
                [(Text)"resetIndex"] = ResetIndex.Serialize(),
                [(Text)"ended"] = Ended.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

        private void ResetOrderedArenaInfos()
        {
            OrderedArenaInfos = _map.Values
                .OrderByDescending(pair => pair.Score)
                .ThenBy(pair => pair.CombatPoint)
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
                info.ResetCount();
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
            var filtered = prevState.Where(i => i.Value.Active).ToList();
#pragma warning restore LAA1002
            foreach (var kv in filtered)
            {
                var value = new ArenaInfo(kv.Value);
                _map[kv.Key] = value;
            }
            ResetIndex = index;
        }

        public void SetReceive(Address avatarAddress)
        {
            _map[avatarAddress].Receive = true;
        }

        #region IDictionary

        public IEnumerator<KeyValuePair<Address, ArenaInfo>> GetEnumerator()
        {
            return _map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Address, ArenaInfo> item)
        {
            _map[item.Key] = item.Value;
            ResetOrderedArenaInfos();
        }

        public void Clear()
        {
            _map.Clear();
            ResetOrderedArenaInfos();
        }

        public bool Contains(KeyValuePair<Address, ArenaInfo> item)
        {
#pragma warning disable LAA1002
            return _map.Contains(item);
#pragma warning restore LAA1002
        }

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
            Add(new KeyValuePair<Address, ArenaInfo>(key, value));
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
            return _map.TryGetValue(key, out value);
        }

        public ArenaInfo this[Address key]
        {
            get => _map[key];
            set
            {
                _map[key] = value;
                ResetOrderedArenaInfos();
            }
        }

        public ICollection<Address> Keys => _map.Keys;
        public ICollection<ArenaInfo> Values => _map.Values;

        #endregion

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }
    }
}
