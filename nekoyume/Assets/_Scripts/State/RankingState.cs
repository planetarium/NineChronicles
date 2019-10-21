using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

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
        
        private readonly Dictionary<Address, AvatarState> _map;

        public RankingState() : base(Address)
        {
            _map = new Dictionary<Address, AvatarState>();
        }

        public RankingState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            _map = ((Bencodex.Types.Dictionary) serialized[(Text) "map"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => new AvatarState((Bencodex.Types.Dictionary) kv.Value)
            );
        }
        
        public void Update(AvatarState state)
        {
            if (_map.TryGetValue(state.address, out var current))
            {
                if (current.worldStage < state.worldStage)
                {
                    _map[state.address] = (AvatarState) state.Clone();
                }
            }
            else
            {
                _map[state.address] = (AvatarState) state.Clone();
            }
        }

        public AvatarState[] GetAvatars(DateTimeOffset? dt)
        {
            IEnumerable<AvatarState> map =
                _map.Values.OrderByDescending(c => c.worldStage).ThenBy(c => c.clearedAt);
            if (dt != null)
            {
                map = map.Where(context => ((TimeSpan) (dt - context.updatedAt)).Days <= 1);
            }

            return map.ToArray();
        }

        public Address[] GetAgentAddresses(int count, DateTimeOffset? dt)
        {
            var avatars = GetAvatars(dt);
            var result = new HashSet<Address>();
            foreach (var avatar in avatars)
            {
                result.Add(avatar.agentAddress);
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
}
