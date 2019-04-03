using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class SelectItemCountPopup<T> : IDisposable where T : Game.Item.Inventory.InventoryItem
    {
        public readonly ReactiveProperty<T> Item = new ReactiveProperty<T>(null);
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<int> MinCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> MaxCount = new ReactiveProperty<int>(1);
        
        public readonly Subject<SelectItemCountPopup<T>> OnClickMinus = new Subject<SelectItemCountPopup<T>>();
        public readonly Subject<SelectItemCountPopup<T>> OnClickPlus = new Subject<SelectItemCountPopup<T>>();
        public readonly Subject<SelectItemCountPopup<T>> OnClickSubmit = new Subject<SelectItemCountPopup<T>>();
        public readonly Subject<SelectItemCountPopup<T>> OnClickClose = new Subject<SelectItemCountPopup<T>>();

        public SelectItemCountPopup()
        {
            MinCount.Subscribe(min =>
            {
                if (Count.Value < min)
                {
                    Count.Value = min;
                }
            });

            MaxCount.Subscribe(max =>
            {
                if (Count.Value > max)
                {
                    Count.Value = max;
                }
            });
            
            OnClickMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.Count.Value <= MinCount.Value)
                {
                    return;
                }

                obj.Count.Value--;
            });
            
            OnClickPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.Count.Value >= MaxCount.Value)
                {
                    return;
                }

                obj.Count.Value++;
            });
        }
        
        public void Dispose()
        {
            Item.Dispose();
            Count.Dispose();
            MinCount.Dispose();
            MaxCount.Dispose();
            
            OnClickMinus.Dispose();
            OnClickPlus.Dispose();
            OnClickSubmit.Dispose();
            OnClickClose.Dispose();
        }
    }
}
