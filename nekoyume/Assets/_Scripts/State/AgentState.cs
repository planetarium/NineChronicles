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
        public readonly Dictionary<int, Address> avatarAddresses;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
        }
    }
}
