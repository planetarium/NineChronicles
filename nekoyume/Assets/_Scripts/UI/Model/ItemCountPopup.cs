using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountPopup<T> : IDisposable where T : ItemCountPopup<T>
    {
        public readonly ReactiveProperty<string> titleText = new ReactiveProperty<string>("수량 선택");
        public readonly ReactiveProperty<CountEditableItem> item = new ReactiveProperty<CountEditableItem>(null);
        public readonly ReactiveProperty<bool> countEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> submitText = new ReactiveProperty<string>("확인");
        
        public readonly Subject<T> onClickMinus = new Subject<T>();
        public readonly Subject<T> onClickPlus = new Subject<T>();
        public readonly Subject<T> onClickSubmit = new Subject<T>();
        public readonly Subject<T> onClickClose = new Subject<T>();

        public ItemCountPopup()
        {
            onClickMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.item.Value.count.Value <= item.Value.minCount.Value)
                {
                    return;
                }

                obj.item.Value.count.Value--;
            });
            
            onClickPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    obj.item.Value.count.Value >= item.Value.maxCount.Value)
                {
                    return;
                }

                obj.item.Value.count.Value++;
            });
        }
        
        public virtual void Dispose()
        {
            titleText.Dispose();
            item.Dispose();
            countEnabled.Dispose();
            submitText.Dispose();
            
            onClickMinus.Dispose();
            onClickPlus.Dispose();
            onClickSubmit.Dispose();
            onClickClose.Dispose();
        }
    }
}
