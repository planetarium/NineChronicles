using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.State
{
    /// <summary>
    /// Ranking의 상태 모델이다.
    ///
    /// 모든 유저를 하나의 해시맵에 담고 있음.
    /// 너무 많은 유저 정보가 쌓이면, 각 유저를 그룹화하고 그룹 별로 해시맵을 관리하는 것이 성능상 이점이 있겠음.
    /// </summary>
    [Serializable]
    public class RankingState : State
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1
            }
        );

        private readonly Dictionary<Address, RankingInfo> _map;

        public RankingState() : base(Address)
        {
            _map = new Dictionary<Address, RankingInfo>();
        }

        public RankingState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            _map = ((Bencodex.Types.Dictionary) serialized["map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new RankingInfo((Bencodex.Types.Dictionary) kv.Value)
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

        public RankingInfo[] GetAvatars(DateTimeOffset? dt)
        {
            IEnumerable<RankingInfo> map =
                _map.Values.OrderByDescending(c => c.Exp).ThenBy(c => c.StageClearedBlockIndex);
            if (dt != null)
            {
                map = map.Where(context => ((TimeSpan) (dt - context.UpdatedAt)).Days <= 1);
            }

            return map.ToArray();
        }

        public Address[] GetAgentAddresses(int count, DateTimeOffset? dt)
        {
            var avatars = GetAvatars(dt);
            var result = new HashSet<Address>();
            foreach (var avatar in avatars)
            {
                result.Add(avatar.AgentAddress);
                if (result.Count == count)
                    break;
            }

            return result.ToArray();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "map"] = new Bencodex.Types.Dictionary(_map.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary) kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                ))
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }

    public class RankingInfo: IState
    {
        public readonly Address AvatarAddress;
        public readonly Address AgentAddress;
        public readonly int ArmorId;
        public readonly int Level;
        public readonly string AvatarName;
        public readonly long Exp;
        public readonly long StageClearedBlockIndex;
        public readonly DateTimeOffset UpdatedAt;

        public RankingInfo(AvatarState avatarState)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
            AvatarName = avatarState.NameWithHash;
            Exp = avatarState.exp;
            avatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(out var detail);
            StageClearedBlockIndex = detail.StageClearedBlockIndex;
            UpdatedAt = avatarState.updatedAt;
        }

        public RankingInfo(Bencodex.Types.Dictionary serialized)
        {
            AvatarAddress = serialized.GetAddress("avatarAddress");
            AgentAddress = serialized.GetAddress("agentAddress");
            ArmorId = serialized.GetInteger("armorId");
            Level = serialized.GetInteger("level");
            AvatarName = serialized.GetString("avatarName");
            Exp = serialized.GetLong("exp");
            StageClearedBlockIndex = serialized.GetLong("stageClearedBlockIndex");
            UpdatedAt = serialized.GetDateTimeOffset("updatedAt");
        }
        public IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Bencodex.Types.Text) "avatarAddress"] = AvatarAddress.Serialize(),
                [(Bencodex.Types.Text) "agentAddress"] = AgentAddress.Serialize(),
                [(Bencodex.Types.Text) "armorId"] = ArmorId.Serialize(),
                [(Bencodex.Types.Text) "level"] = Level.Serialize(),
                [(Bencodex.Types.Text) "avatarName"] = AvatarName.Serialize(),
                [(Bencodex.Types.Text) "exp"] = Exp.Serialize(),
                [(Bencodex.Types.Text) "stageClearedBlockIndex"] = StageClearedBlockIndex.Serialize(),
                [(Bencodex.Types.Text) "updatedAt"] = UpdatedAt.Serialize(),
            });
    }
}
