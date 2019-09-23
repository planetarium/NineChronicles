using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State, ICloneable
    {
        //F&F 테스트용 노마이너 기본 소지 골드
        public decimal gold = 1000;
        public readonly Dictionary<int, Address> avatarAddresses;

        public AgentState(Address address) : base(address)
        {
            avatarAddresses = new Dictionary<int, Address>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
