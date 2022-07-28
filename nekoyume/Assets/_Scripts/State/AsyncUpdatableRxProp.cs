using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Nekoyume.State
{
    using UniRx;

    public interface IReadOnlyAsyncUpdatableRxProp<T> : IReadOnlyReactiveProperty<T>
    {
        bool IsUpdating { get; }

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
        private readonly Func<T, Task<T>> _updateAsyncFunc;

        public bool IsUpdating { get; private set; } = false;

        public AsyncUpdatableRxProp(Func<T, Task<T>> updateAsyncFunc) :
            this(default, updateAsyncFunc)
        {
        }

        public AsyncUpdatableRxProp(T defaultValue, Func<T, Task<T>> updateAsyncFunc)
        {
            Value = defaultValue;
            _updateAsyncFunc = updateAsyncFunc
                               ?? throw new ArgumentNullException(nameof(updateAsyncFunc));
        }

        public async UniTask<T> UpdateAsync(bool forceNotify = false)
        {
            IsUpdating = true;
            var t = await Task.Run(async () =>
                await _updateAsyncFunc(Value));
            IsUpdating = false;
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
