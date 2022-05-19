using System;
using Cysharp.Threading.Tasks;

namespace Nekoyume.State
{
    using UniRx;

    public class AsyncUpdatableRxProp<TValue> :
        ReactiveProperty<TValue>,
        IAsyncUpdatableReadOnlyRxProp<TValue>
    {
        private readonly Func<TValue, UniTask<TValue>> _updateAsyncFunc;

        public AsyncUpdatableRxProp(Func<TValue, UniTask<TValue>> updateAsyncFunc) :
            this(default, updateAsyncFunc)
        {
        }

        public AsyncUpdatableRxProp(TValue defaultValue, Func<TValue, UniTask<TValue>> updateAsyncFunc)
        {
            Value = defaultValue;
            _updateAsyncFunc = updateAsyncFunc
                               ?? throw new ArgumentNullException(nameof(updateAsyncFunc));
        }

        public async UniTaskVoid UpdateAsync(bool forceNotify)
        {
            var t = await _updateAsyncFunc(Value);
            SetValue(t, forceNotify);
        }

        public IObservable<TValue> UpdateAsObservable(bool forceNotify)
        {
            var observable = _updateAsyncFunc(Value).ToObservable();
            observable.First().Subscribe(t => SetValue(t, forceNotify));
            return this.AsObservable();
        }

        public IDisposable SubscribeWithUpdateOnce(Action<TValue> onNext)
        {
            UpdateAsync(false).Forget();
            return this.Subscribe(onNext);
        }

        private void SetValue(TValue value, bool forceNotify)
        {
            if (forceNotify)
            {
                SetValueAndForceNotify(value);
            }
            else
            {
                SetValue(value);
            }
        }
    }
}
