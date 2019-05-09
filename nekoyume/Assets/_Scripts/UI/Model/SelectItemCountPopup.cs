using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class SelectItemCountPopup : IDisposable
    {
        public readonly ReactiveProperty<CountableItem> item = new ReactiveProperty<CountableItem>(null);
        public readonly ReactiveProperty<int> count = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<int> minCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> maxCount = new ReactiveProperty<int>(1);
        
        public readonly Subject<SelectItemCountPopup> onClickMinus = new Subject<SelectItemCountPopup>();
        public readonly Subject<SelectItemCountPopup> onClickPlus = new Subject<SelectItemCountPopup>();
        public readonly Subject<SelectItemCountPopup> onClickSubmit = new Subject<SelectItemCountPopup>();
        public readonly Subject<SelectItemCountPopup> onClickClose = new Subject<SelectItemCountPopup>();

        public SelectItemCountPopup()
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
