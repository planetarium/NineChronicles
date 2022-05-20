using System;
using Cysharp.Threading.Tasks;

namespace Nekoyume.State
{
    using UniRx;

    public interface IReadOnlyAsyncUpdatableRxProp<T> : IReadOnlyReactiveProperty<T>
    {
        UniTask<T> UpdateAsync(bool forceNotify = false);

        IObservable<T> UpdateAsObservable(bool forceNotify = false);

        IDisposable SubscribeWithUpdateOnce(Action<T> onNext, bool forceNotify = false);

        IDisposable SubscribeOnMainThreadWithUpdateOnce(
            Action<T> onNext,
            bool forceNotify = false);
    }

    public interface IAsyncUpdatableRxProp<T> : IReadOnlyAsyncUpdatableRxProp<T>
    {
        new T Value { get; set; }
    }

    public class AsyncUpdatableRxProp<T> :
        ReactiveProperty<T>,
        IAsyncUpdatableRxProp<T>
    {
        private readonly Func<T, UniTask<T>> _updateAsyncFunc;

        public AsyncUpdatableRxProp(Func<T, UniTask<T>> updateAsyncFunc) :
            this(default, updateAsyncFunc)
        {
        }

        public AsyncUpdatableRxProp(T defaultValue, Func<T, UniTask<T>> updateAsyncFunc)
        {
            Value = defaultValue;
            _updateAsyncFunc = updateAsyncFunc
                               ?? throw new ArgumentNullException(nameof(updateAsyncFunc));
        }

        public async UniTask<T> UpdateAsync(bool forceNotify = false)
        {
            var t = await _updateAsyncFunc(Value);
            if (forceNotify)
            {
                SetValueAndForceNotify(t);
            }
            else
            {
                Value = t;
            }

            return t;
        }

        public IObservable<T> UpdateAsObservable(bool forceNotify = false) =>
            UpdateAsync(forceNotify).ToObservable();

        public IDisposable SubscribeWithUpdateOnce(Action<T> onNext, bool forceNotify = false)
        {
            UpdateAsync(forceNotify).Forget();
            return this.Subscribe(onNext);
        }

        public IDisposable SubscribeOnMainThreadWithUpdateOnce(
            Action<T> onNext,
            bool forceNotify = false)
        {
            UpdateAsync(forceNotify).Forget();
            return this
                .SubscribeOnMainThread()
                .Subscribe(onNext);
        }
    }
}
