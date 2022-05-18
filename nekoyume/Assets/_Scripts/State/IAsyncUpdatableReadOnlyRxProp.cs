using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Nekoyume.State
{
    public interface IAsyncUpdatableReadOnlyRxProp<T> : IReadOnlyReactiveProperty<T>
    {
        UniTaskVoid UpdateAsync(bool forceNotify);

        IObservable<T> UpdateAsObservable(bool forceNotify);

        IDisposable SubscribeWithUpdateOnce(Action<T> onNext);
    }
}
