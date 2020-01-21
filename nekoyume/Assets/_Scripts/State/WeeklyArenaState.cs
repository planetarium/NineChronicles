using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Game.Item;

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
        public ArenaInfo Active(AvatarState avatarState, decimal i)
        {
            Gold += i;
            Update(avatarState, true);
            return _map[avatarState.address];
        }

        public void Update(ArenaInfo info)
        {
            Add(info.AvatarAddress, info);
        }

        public void Set(AvatarState avatarState)
        {
            Update(avatarState);
        }

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

        public void ResetCount(long ctxBlockIndex)
        {
            var map = _map.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var pair in map)
            {
                var info = pair.Value;
                info.DailyChallengeCount = 5;
                _map[pair.Key] = info;
            }

            ResetIndex = ctxBlockIndex;
        }
    }

    public class ArenaInfo : IState
    {
        public readonly Address AvatarAddress;
        public readonly Address AgentAddress;
        public readonly int Level;
        public readonly string AvatarName;
        public readonly int CombatPoint;
        public readonly bool Active;
        public int DailyChallengeCount;
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
            });

        public void Update(int score)
        {
            var calculated = Score + score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
        }

        public void Update(AvatarState state)
        {
            var armor = state.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
        }
    }
}
