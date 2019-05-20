using System;
using System.Collections.Generic;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.Model
{
    public static class Agent
    {
        public static readonly ReactiveProperty<decimal> Gold = new ReactiveProperty<decimal>(0);
        
        private static readonly List<IDisposable> Disposables = new List<IDisposable>();

        static Agent()
        {
            // FixMe. 골드 동기화 방법 수정 필요.
            RewardGold.RewardGoldMyselfSubject.ObserveOnMainThread().Subscribe(_ => Gold.Value = _).AddTo(Disposables);
        }
    }
}
