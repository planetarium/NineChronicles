using Nekoyume.State;
using UniRx;

namespace Nekoyume.Model
{
    /// <summary>
    /// AgentState가 포함하는 값의 변화를 ActionBase.EveryRender<T>()를 통해 감지하고, 동기화한다.
    /// 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveAgentState
    {
        public static readonly ReactiveProperty<decimal> Gold = new ReactiveProperty<decimal>(0);

        public static void Initialize(AgentState agentState)
        {
            if (ReferenceEquals(agentState, null))
            {
                return;
            }
            
            Gold.Value = agentState.gold;
        }
    }
}
