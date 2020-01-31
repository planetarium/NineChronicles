using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using DecimalMath;
using Libplanet;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.WeeklyArena;

namespace Nekoyume.Model.State
{
    public class WeeklyArenaState : State, IDictionary<Address, ArenaInfo>
    {
        #region static

        private static List<Address> _addresses = null;

        public static List<Address> Addresses
        {
            get
            {
                if (!(_addresses is null))
                    return _addresses;

                _addresses = new List<Address>();
                for (byte i = 0x10; i < 0x62; i++)
                {
                    var addr = new Address(new byte[]
                    {
                        0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, i
                    });
                    _addresses.Add(addr);
                }

                return _addresses;
            }
        }

        #endregion

        public decimal Gold;

        public long ResetIndex;

        private readonly Dictionary<Address, ArenaInfo> _map;
        private Dictionary<TierType, decimal> _rewardMap = new Dictionary<TierType, decimal>();

        public WeeklyArenaState(Address address) : base(address)
        {
            _map = new Dictionary<Address, ArenaInfo>();
        }

        public WeeklyArenaState(Dictionary serialized) : base(serialized)
        {
            _map = ((Dictionary)serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new ArenaInfo((Dictionary)kv.Value)
            );

            ResetIndex = serialized.GetLong("resetIndex");

            if (serialized.ContainsKey((Text)"rewardMap"))
            {
                _rewardMap = ((Dictionary)serialized["rewardMap"]).ToDictionary(
                    kv => (TierType)((Binary)kv.Key).First(),
                    kv => kv.Value.ToDecimal());
            }

            Gold = serialized.GetDecimal("gold");
        }

        public WeeklyArenaState(IValue iValue) : this((Dictionary)iValue)
        {
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"map"] = new Dictionary(_map.Select(kv =>
                   new KeyValuePair<IKey, IValue>(
                       (Binary)kv.Key.Serialize(),
                       kv.Value.Serialize()
                   )
                )),
                [(Text)"resetIndex"] = ResetIndex.Serialize(),
                [(Text)"rewardMap"] = new Dictionary(_rewardMap.Select(kv =>
                   new KeyValuePair<IKey, IValue>(
                       new Binary(new[] { (byte)kv.Key }),
                       kv.Value.Serialize()
                   )
                )),
                [(Text)"gold"] = Gold.Serialize(),
            }.Union((Dictionary)base.Serialize()));

        /// <summary>
        /// 인자로 넘겨 받은 `avatarAddress`를 기준으로 상위와 하위 범위에 해당하는 랭킹 정보를 얻습니다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="upperRange">상위 범위</param>
        /// <param name="lowerRange">하위 범위</param>
        /// <returns></returns>
        public List<(int rank, ArenaInfo arenaInfo)> GetArenaInfos(Address avatarAddress, int upperRange = 10,
            int lowerRange = 10)
        {
            var arenaInfos = _map.Values
                .OrderByDescending(pair => pair.Score)
                .ThenBy(pair => pair.CombatPoint)
                .ToList();

            var avatarIndex = 0;
            for (var i = 0; i < arenaInfos.Count; i++)
            {
                var pair = arenaInfos[i];
                if (!pair.AvatarAddress.Equals(avatarAddress))
                    continue;

                avatarIndex = i;
                break;
            }

            var firstIndex = Math.Max(0, avatarIndex - upperRange);
            var lastIndex = Math.Min(avatarIndex + lowerRange, arenaInfos.Count - 1);
            var offsetIndex = 1;
            return arenaInfos.GetRange(firstIndex, lastIndex - firstIndex + 1)
                .Select(arenaInfo => (firstIndex + offsetIndex++, arenaInfo))
                .ToList();
        }

        public ArenaInfo GetArenaInfo(Address avatarAddress)
        {
            return _map.Values.FirstOrDefault(info => info.AvatarAddress.Equals(avatarAddress));
        }

        private void Update(AvatarState avatarState, bool active = false)
        {
            Add(avatarState.address, new ArenaInfo(avatarState, active));
        }

        public void Update(ArenaInfo info)
        {
            Add(info.AvatarAddress, info);
        }

        public void Set(AvatarState avatarState)
        {
            Update(avatarState);
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
            SetRewardMap();
        }

        public void Update(WeeklyArenaState prevState, long index)
        {
            var filtered = prevState.Where(i => i.Value.Active).ToList();
            foreach (var kv in filtered)
            {
                var value = new ArenaInfo(kv.Value);
                _map[kv.Key] = value;
            }
            ResetIndex = index;
        }

        public TierType GetTier(ArenaInfo info)
        {
            var sorted = _map.Values.Where(i => i.Active).OrderBy(i => i.Score).ThenBy(i => i.CombatPoint).ToList();
            if (info.ArenaRecord.Win >= 5)
            {
                return TierType.Platinum;
            }

            if (info.ArenaRecord.Win >= 4)
            {
                return TierType.Gold;
            }

            if (info.ArenaRecord.Win >= 3)
            {
                return TierType.Silver;
            }

            if (info.ArenaRecord.Win >= 2)
            {
                return TierType.Bronze;
            }

            return TierType.Rookie;
        }

        private void SetRewardMap()
        {
            var map = new Dictionary<TierType, decimal>
            {
                [TierType.Platinum] = 200m,
                [TierType.Gold] = 150m,
                [TierType.Silver] = 100m,
                [TierType.Bronze] = 80m,
                [TierType.Rookie] = 70m
            };
            _rewardMap = map;
        }
        public decimal GetReward(TierType tier)
        {
            return _rewardMap[tier];
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
        }

        public void Clear()
        {
            _map.Clear();
        }

        public bool Contains(KeyValuePair<Address, ArenaInfo> item)
        {
            return _map.Contains(item);
        }

        public void CopyTo(KeyValuePair<Address, ArenaInfo>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Address, ArenaInfo> item)
        {
            return _map.Remove(item.Key);
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
            return _map.Remove(key);
        }

        public bool TryGetValue(Address key, out ArenaInfo value)
        {
            return _map.TryGetValue(key, out value);
        }

        public ArenaInfo this[Address key]
        {
            get => _map[key];
            set => _map[key] = value;
        }

        public ICollection<Address> Keys => _map.Keys;
        public ICollection<ArenaInfo> Values => _map.Values;

        #endregion
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
        public readonly Address AgentAddress;
        public readonly string AvatarName;
        public readonly Record ArenaRecord;
        public int Level { get; private set; }
        public int CombatPoint { get; private set; }
        public int ArmorId { get; private set; }
        public bool Active { get; private set; }
        public int DailyChallengeCount { get; private set; }
        public int Score { get; private set; }
        public bool Receive;

        public ArenaInfo(AvatarState avatarState, bool active)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            AvatarName = avatarState.NameWithHash;
            ArenaRecord = new Record();
            Level = avatarState.level;
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            CombatPoint = CPHelper.GetCP(avatarState);
            Active = active;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Score = GameConfig.ArenaScoreDefault;
        }

        public ArenaInfo(Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            AvatarName = serialized.GetString("avatarName");
            ArenaRecord = serialized.ContainsKey((Text)"arenaRecord")
                ? new Record((Dictionary)serialized["arenaRecord"])
                : new Record();
            Level = serialized.GetInteger("level");
            ArmorId = serialized.GetInteger("armorId");
            CombatPoint = serialized.GetInteger("combatPoint");
            Active = serialized.GetBoolean("active");
            DailyChallengeCount = serialized.GetInteger("dailyChallengeCount");
            Score = serialized.GetInteger("score");
        }

        public ArenaInfo(ArenaInfo prevInfo)
        {
            AvatarAddress = prevInfo.AvatarAddress;
            AgentAddress = prevInfo.AgentAddress;
            ArmorId = prevInfo.ArmorId;
            Level = prevInfo.Level;
            AvatarName = prevInfo.AvatarName;
            CombatPoint = 100;
            Score = 1000;
            DailyChallengeCount = 5;
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
            });

        public void Update(int score)
        {
            var calculated = Score + score;
            Score = Math.Max(GameConfig.ArenaScoreDefault, calculated);
            DailyChallengeCount--;
        }

        public void Update(AvatarState state)
        {
            ArmorId = state.GetArmorId();
            CombatPoint = CPHelper.GetCP(state);
        }

        public int Update(AvatarState avatarState, ArenaInfo enemyInfo, BattleLog.Result result)
        {
            int score;
            switch (result)
            {
                case BattleLog.Result.Win:
                    score = GameConfig.BaseVictoryPoint;
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    score = GameConfig.BaseDefeatPoint;
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var rating = Score;
            var enemyRating = enemyInfo.Score;
            if (rating != enemyRating)
            {
                switch (result)
                {
                    case BattleLog.Result.Win:
                        score = (int)(DecimalEx.Pow((decimal)enemyRating / rating, 0.75m) *
                                       GameConfig.BaseVictoryPoint);
                        break;
                    case BattleLog.Result.Lose:
                        score = (int)(DecimalEx.Pow((decimal)rating / enemyRating, 0.75m) *
                                       GameConfig.BaseDefeatPoint);
                        break;
                }
            }

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
            DailyChallengeCount = 5;
        }
    }
}
