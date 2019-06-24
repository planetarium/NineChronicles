using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State
    {
        public decimal gold;
        // 지금은 key로 int형을 사용하지만, Address형을 직접 사용하는 편이 유연할 것으로 보임.
        public readonly Dictionary<int, Address> avatarAddresses;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
        }
    }
}
