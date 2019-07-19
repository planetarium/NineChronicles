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
        //노마이너 상점 구매 테스트를 위한 기본 골드
        public decimal gold = 1;
        public readonly Dictionary<int, Address> avatarAddresses;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
        }
    }
}
