using System;
using Nekoyume.Model.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.State.Subjects
{
    /// <summary>
    /// 기존 `ReactiveXXX` 객체들이 `ReactiveProperty<T>` 필드에 값을 직접 들고 있는 것과 달리 `Subject<T>`를 사용해서 전달에만 목적을 둔다.
    /// 기존 `ReactiveXXX` 객체들도 `XXXSubject` 객체로 바꿀지는 사용성을 보면서 결정한다.
    /// </summary>
    public static class WeeklyArenaStateSubject
    {
        private static readonly Subject<WeeklyArenaState> WeeklyArenaStateInternal;
        private static readonly Subject<long> ResetIndexInternal;
        
        public static readonly IObservable<WeeklyArenaState> WeeklyArenaState;
        public static readonly IObservable<long> ResetIndex;
        
        static WeeklyArenaStateSubject()
        {
            WeeklyArenaStateInternal = new Subject<WeeklyArenaState>();
            ResetIndexInternal = new Subject<long>();
            WeeklyArenaState = WeeklyArenaStateInternal.ObserveOnMainThread();
            ResetIndex = ResetIndexInternal.ObserveOnMainThread();
        }

        public static void OnNext(WeeklyArenaState state)
        {
            if (state is null)
            {
                NcDebug.LogWarning($"[{nameof(WeeklyArenaStateSubject)}] / {nameof(OnNext)} / {nameof(state)} is null.");
                return;
            }

            WeeklyArenaStateInternal.OnNext(state);
            ResetIndexInternal.OnNext(state.ResetIndex);
        }
    }
}
