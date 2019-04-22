using System;
using System.Collections.Generic;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.Model
{
    public static class Agent
    {
        public static readonly ReactiveProperty<int> Gold = new ReactiveProperty<int>(0);
        
        private static readonly List<IDisposable> Disposables = new List<IDisposable>();

        static Agent()
        {
            RewardGold.RewardGoldMyselfSubject.ObserveOnMainThread().Subscribe(_ => Gold.Value = _).AddTo(Disposables);
        }
    }
}
