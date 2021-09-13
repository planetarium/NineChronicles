using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
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

    public class ArenaInfo : IState
    {
        public class Record : IState
        {
            public int Win;
            public int Lose;
            public int Draw;

            public Record()
            {
            }

            public Record(Dictionary serialized)
            {
                Win = serialized.GetInteger("win");
                Lose = serialized.GetInteger("lose");
                Draw = serialized.GetInteger("draw");
            }

            public IValue Serialize() =>
                Dictionary.Empty
                    .Add("win", Win.Serialize())
                    .Add("lose", Lose.Serialize())
                    .Add("draw", Draw.Serialize());
        }

        public readonly Address AvatarAddress;
        public readonly Record ArenaRecord;
        public int Score { get; private set; }
        public int DailyChallengeCount { get; private set; }
        public bool Active { get; private set; }

        [Obsolete("Not used anymore since v100070")]
        public readonly Address AgentAddress;

        [Obsolete("Not used anymore since v100070")]
        public bool Receive;

        [Obsolete("Not used anymore since v100070")]
        public readonly string AvatarName;

        [Obsolete("Not used anymore since v100070")]
        public int Level { get; private set; }

        [Obsolete("Not used anymore since v100070")]
        public int CombatPoint { get; private set; }

        [Obsolete("Not used anymore since v100070")]
        public int ArmorId { get; private set; }



        public ArenaInfo(AvatarState avatarState, CharacterSheet characterSheet, bool active)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            AvatarName = avatarState.NameWithHash;
            ArenaRecord = new Record();
            Level = avatarState.level;
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            CombatPoint = CPHelper.GetCP(avatarState, characterSheet);
            Active = active;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Score = GameConfig.ArenaScoreDefault;
        }

        public ArenaInfo(AvatarState avatarState, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet, bool active)
            : this(avatarState, characterSheet, active)
        {
            CombatPoint = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
        }

        public ArenaInfo(Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            AvatarName = serialized.GetString("avatarName");
            ArenaRecord = serialized.ContainsKey((IKey)(Text)"arenaRecord")
                ? new Record((Dictionary)serialized["arenaRecord"])
                : new Record();
            Level = serialized.GetInteger("level");
            ArmorId = serialized.GetInteger("armorId");
            CombatPoint = serialized.GetInteger("combatPoint");
            Active = serialized.GetBoolean("active");
            DailyChallengeCount = serialized.GetInteger("dailyChallengeCount");
            Score = serialized.GetInteger("score");
            Receive = serialized["receive"].ToBoolean();
        }

        public ArenaInfo(ArenaInfo prevInfo)
        {
            AvatarAddress = prevInfo.AvatarAddress;
            AgentAddress = prevInfo.AgentAddress;
            ArmorId = prevInfo.ArmorId;
            Level = prevInfo.Level;
            AvatarName = prevInfo.AvatarName;
            CombatPoint = prevInfo.CombatPoint;
            Score = 1000;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Active = false;
            ArenaRecord = new Record();
        }

        public IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"avatarAddress"] = AvatarAddress.Serialize(),
                [(Text)"agentAddress"] = AgentAddress.Serialize(),
                [(Text)"avatarName"] = AvatarName.Serialize(),
                [(Text)"arenaRecord"] = ArenaRecord.Serialize(),
                [(Text)"level"] = Level.Serialize(),
                [(Text)"armorId"] = ArmorId.Serialize(),
                [(Text)"combatPoint"] = CombatPoint.Serialize(),
                [(Text)"active"] = Active.Serialize(),
                [(Text)"dailyChallengeCount"] = DailyChallengeCount.Serialize(),
                [(Text)"score"] = Score.Serialize(),
                [(Text)"receive"] = Receive.Serialize(),
            });

        [Obsolete("Use Update()")]
        public void UpdateV1(AvatarState state, CharacterSheet characterSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCP(state, characterSheet);
        }

        [Obsolete("Use Update()")]
        public void UpdateV2(AvatarState state, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCPV2(state, characterSheet, costumeStatSheet);
        }
        public int Update(AvatarState avatarState, ArenaInfo enemyInfo, BattleLog.Result result)
        {
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var score = ArenaScoreHelper.GetScoreV1(Score, enemyInfo.Score, result);
            var calculated = Score + score;
            var current = Score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
            return Score - current;
        }

        public void Activate()
        {
            Active = true;
        }

        public void ResetCount()
        {
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
        }

        public int GetRewardCount()
        {
            if (Score >= 1800)
            {
                return 6;
            }

            if (Score >= 1400)
            {
                return 5;
            }

            if (Score >= 1200)
            {
                return 4;
            }

            if (Score >= 1100)
            {
                return 3;
            }

            if (Score >= 1001)
            {
                return 2;
            }

            return 1;
        }
    }
}
