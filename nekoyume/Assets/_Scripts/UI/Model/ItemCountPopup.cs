using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountPopup<T> : IDisposable where T : ItemCountPopup<T>
    {
        public readonly ReactiveProperty<CountableItem> item = new ReactiveProperty<CountableItem>(null);
        public readonly ReactiveProperty<int> count = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<int> minCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> maxCount = new ReactiveProperty<int>(1);
        
        public readonly Subject<T> onClickMinus = new Subject<T>();
        public readonly Subject<T> onClickPlus = new Subject<T>();
        public readonly Subject<T> onClickSubmit = new Subject<T>();
        public readonly Subject<T> onClickClose = new Subject<T>();

        public ItemCountPopup()
        {
            minCount.Subscribe(min =>
            {
                if (count.Value < min)
                {
                    count.Value = min;
                }
            });

            maxCount.Subscribe(max =>
            {
                if (count.Value > max)
                {
                    count.Value = max;
                }
            });
            
            onClickMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.count.Value <= minCount.Value)
                {
                    return;
                }

                obj.count.Value--;
            });
            
            onClickPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.count.Value >= maxCount.Value)
                {
                    return;
                }

                obj.count.Value++;
            });
        }
        
        public virtual void Dispose()
        {
            item.Dispose();
            count.Dispose();
            minCount.Dispose();
            maxCount.Dispose();
            
            onClickMinus.Dispose();
            onClickPlus.Dispose();
            onClickSubmit.Dispose();
            onClickClose.Dispose();
        }
    }
}
