using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using DecimalMath;
using Libplanet;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.State
{
    public class WeeklyArenaState : State, IDictionary<Address, ArenaInfo>
    {
        public static List<Address> Addresses
        {
            get
            {
                var addresses = new List<Address>();
                for (byte i = 0x10; i < 0x62; i++)
                {
                    var addr = new Address(new byte[]
                    {
                        0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, i
                    });
                    addresses.Add(addr);
                }

                return addresses;
            }
        }

        public decimal Gold;

        public long ResetIndex;

        private readonly Dictionary<Address, ArenaInfo> _map;

        public WeeklyArenaState(Address address) : base(address)
        {
            _map = new Dictionary<Address, ArenaInfo>();
            Gold = 100;
        }

        public WeeklyArenaState(Dictionary serialized) : base(serialized)
        {
            _map = ((Bencodex.Types.Dictionary) serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new ArenaInfo((Bencodex.Types.Dictionary) kv.Value)
            );

            ResetIndex = serialized.GetLong("resetIndex");
        }
        
        public WeeklyArenaState(IValue iValue) : this((Bencodex.Types.Dictionary) iValue)
        {
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "map"] = new Bencodex.Types.Dictionary(_map.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary) kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                )),
                [(Text) "resetIndex"] = ResetIndex.Serialize()
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        
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
            throw new System.NotImplementedException();
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

            public Record(Bencodex.Types.Dictionary serialized)
            {
                Win = serialized.GetInteger("win");
                Lose = serialized.GetInteger("lose");
                Draw = serialized.GetInteger("draw");
            }

            public IValue Serialize() =>
                Bencodex.Types.Dictionary.Empty
                    .Add("win", Win.Serialize())
                    .Add("lose", Lose.Serialize())
                    .Add("draw", Draw.Serialize());
        }

        public readonly Address AvatarAddress;
        public readonly Address AgentAddress;
        public readonly string AvatarName;
        public readonly int CombatPoint;
        public readonly Record ArenaRecord;
        public int Level { get; private set; }
        public bool Active { get; private set; }
        public int DailyChallengeCount { get; private set; }
        public int ArmorId { get; private set; }
        public int Score { get; private set; }

        public ArenaInfo(AvatarState avatarState, bool active)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            Level = avatarState.level;
            AvatarName = avatarState.NameWithHash;
            CombatPoint = 100;
            Score = 1000;
            DailyChallengeCount = 5;
            Active = active;
            ArenaRecord = new Record();
        }

        public ArenaInfo(Bencodex.Types.Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            ArmorId = serialized.GetInteger("armorId");
            Level = serialized.GetInteger("level");
            AvatarName = serialized.GetString("avatarName");
            CombatPoint = serialized.GetInteger("combatPoint");
            Score = serialized.GetInteger("score");
            DailyChallengeCount = serialized.GetInteger("dailyChallengeCount");
            Active = serialized.GetBoolean("active");
            ArenaRecord = serialized.ContainsKey((Text) "arenaRecord")
                ? new Record((Bencodex.Types.Dictionary) serialized["arenaRecord"])
                : new Record();
        }

        public IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Bencodex.Types.Text) "avatarAddress"] = AvatarAddress.Serialize(),
                [(Bencodex.Types.Text) "agentAddress"] = AgentAddress.Serialize(),
                [(Bencodex.Types.Text) "armorId"] = ArmorId.Serialize(),
                [(Bencodex.Types.Text) "level"] = Level.Serialize(),
                [(Bencodex.Types.Text) "avatarName"] = AvatarName.Serialize(),
                [(Bencodex.Types.Text) "combatPoint"] = CombatPoint.Serialize(),
                [(Bencodex.Types.Text) "score"] = Score.Serialize(),
                [(Bencodex.Types.Text) "dailyChallengeCount"] = DailyChallengeCount.Serialize(),
                [(Bencodex.Types.Text) "active"] = Active.Serialize(),
                [(Bencodex.Types.Text) "arenaRecord"] = ArenaRecord.Serialize(),
            });

        public void Update(int score)
        {
            var calculated = Score + score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
        }

        public void Update(AvatarState state)
        {
            ArmorId = state.GetArmorId();
        }

        public void Update(AvatarState avatarState, ArenaInfo enemyInfo, BattleLog.Result result)
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
                    return;
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
                        score = (int) (DecimalEx.Pow((decimal) enemyRating / rating, 0.75m) *
                                       GameConfig.BaseVictoryPoint);
                        break;
                    case BattleLog.Result.Lose:
                        score = (int) (DecimalEx.Pow((decimal) rating / enemyRating, 0.75m) *
                                       GameConfig.BaseVictoryPoint);
                        break;
                }
            }

            var calculated = Score + score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
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
