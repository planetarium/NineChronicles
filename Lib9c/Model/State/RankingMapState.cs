using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    public class RankingMapState : State
    {
        public const int Capacity = 500;

        private readonly Dictionary<Address, RankingInfo> _map;

        public RankingMapState(Address address) : base(address)
        {
            _map = new Dictionary<Address, RankingInfo>();
        }

        public RankingMapState(Dictionary serialized) : base(serialized)
        {
            _map = ((Dictionary)serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new RankingInfo((Dictionary)kv.Value)
            );

        }

        public void Update(AvatarState state)
        {
            if (_map.TryGetValue(state.address, out var current))
            {
                if (current.Exp < state.exp)
                {
                    _map[state.address] = new RankingInfo(state);
                }
            }
            else
            {
                _map[state.address] = new RankingInfo(state);
            }
        }

        public List<RankingInfo> GetRankingInfos(long? blockOffset)
        {
            var list = _map.Values
                .OrderByDescending(c => c.Exp)
                .ThenBy(c => c.StageClearedBlockIndex)
                .ToList();
            return blockOffset != null
                ? list
                    .Where(context => blockOffset <= context.UpdatedAt)
                    .ToList()
                : list;
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "map"] = new Dictionary(_map.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary) kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                ))
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }
    }

    public class RankingInfo : IState
    {
        protected bool Equals(RankingInfo other)
        {
            return AvatarAddress.Equals(other.AvatarAddress) && AgentAddress.Equals(other.AgentAddress) &&
                   ArmorId == other.ArmorId && Level == other.Level && AvatarName == other.AvatarName &&
                   Exp == other.Exp && StageClearedBlockIndex == other.StageClearedBlockIndex &&
                   UpdatedAt.Equals(other.UpdatedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RankingInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AvatarAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ AgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ ArmorId;
                hashCode = (hashCode * 397) ^ Level;
                hashCode = (hashCode * 397) ^ (AvatarName != null ? AvatarName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Exp.GetHashCode();
                hashCode = (hashCode * 397) ^ StageClearedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ UpdatedAt.GetHashCode();
                return hashCode;
            }
        }

        public readonly Address AvatarAddress;
        public readonly Address AgentAddress;
        public readonly int ArmorId;
        public readonly int Level;
        public readonly string AvatarName;
        public readonly long Exp;
        public readonly long StageClearedBlockIndex;
        public readonly long UpdatedAt;

        public RankingInfo(AvatarState avatarState)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
            AvatarName = avatarState.NameWithHash;
            Exp = avatarState.exp;
            avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var detail);
            StageClearedBlockIndex = detail.StageClearedBlockIndex;
            UpdatedAt = avatarState.updatedAt;
        }

        public RankingInfo(Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            ArmorId = serialized.GetInteger("armorId");
            Level = serialized.GetInteger("level");
            AvatarName = serialized.GetString("avatarName");
            Exp = serialized.GetLong("exp");
            StageClearedBlockIndex = serialized.GetLong("stageClearedBlockIndex");
            UpdatedAt = serialized.GetLong("updatedAt");
        }
        public IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"avatarAddress"] = AvatarAddress.Serialize(),
                [(Text)"agentAddress"] = AgentAddress.Serialize(),
                [(Text)"armorId"] = ArmorId.Serialize(),
                [(Text)"level"] = Level.Serialize(),
                [(Text)"avatarName"] = AvatarName.Serialize(),
                [(Text)"exp"] = Exp.Serialize(),
                [(Text)"stageClearedBlockIndex"] = StageClearedBlockIndex.Serialize(),
                [(Text)"updatedAt"] = UpdatedAt.Serialize(),
            });
    }
}
