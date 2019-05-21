using System;
using System.Collections.Generic;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.Model
{
    public class AgentContext : IDisposable
    {
        public readonly ReactiveProperty<decimal> gold = new ReactiveProperty<decimal>(0);
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public AgentContext(State.AgentState agentState)
        {
            gold.Value = agentState.gold;
            
            Subscribes();
        }

        private void Subscribes()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(eval => eval.InputContext.Signer == AddressBook.Agent.Value)
                .Subscribe(eval => gold.Value += eval.Action.gold)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.DisposeAllAndClear();
            
            gold?.Dispose();
        }
    }
}
