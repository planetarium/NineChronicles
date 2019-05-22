using System;
using System.Collections.Generic;
using Nekoyume.Action;
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

        static ReactiveAgentState()
        {
            Subscribes();
        }

        public static void Initialize(AgentState agentState)
        {
            if (ReferenceEquals(agentState, null))
            {
                return;
            }
            
            Gold.Value = agentState.gold;
        }

        private static void Subscribes()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == AddressBook.Agent.Value)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    States.Agent.Value.gold += eval.Action.gold;
                    Gold.Value = States.Agent.Value.gold;
                });
        }
    }
}
