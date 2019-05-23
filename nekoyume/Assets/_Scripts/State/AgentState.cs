using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState
    {
        // ToDo. Avatar 주소들을 갖고 있도록. 이 주소를 통해서 블록에서 꺼내오도록.
        // public List<Address> avatarAddresses;
        
        public decimal gold;
    }
}
