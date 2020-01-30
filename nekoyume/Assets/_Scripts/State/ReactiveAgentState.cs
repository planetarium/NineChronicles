using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State
{
    /// <summary>
    /// AgentState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveAgentState
    {
        public static readonly ReactiveProperty<decimal> Gold = new ReactiveProperty<decimal>(0);
        
        private static Dictionary<int, Address> _avatarAddresses;
        
        public static IReadOnlyDictionary<int, Address> AvatarAddresses => _avatarAddresses;
        
        public static void Initialize(AgentState state)
        {
            if (state is null)
                return;
            
            Gold.Value = state.gold;
            _avatarAddresses = state.avatarAddresses;
        }
    }
}
