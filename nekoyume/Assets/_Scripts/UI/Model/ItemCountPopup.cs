using System;
using Assets.SimpleLocalization;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountPopup<T> : IDisposable where T : ItemCountPopup<T>
    {
        public static readonly string OK = LocalizationManager.Localize("UI_OK");
        
        public readonly ReactiveProperty<string> titleText = new ReactiveProperty<string>("");
        public readonly ReactiveProperty<CountEditableItem> item = new ReactiveProperty<CountEditableItem>(null);
        public readonly ReactiveProperty<bool> countEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> submitText = new ReactiveProperty<string>(OK);
        
        public readonly Subject<T> onClickMinus = new Subject<T>();
        public readonly Subject<T> onClickPlus = new Subject<T>();
        public readonly Subject<T> onClickSubmit = new Subject<T>();
        public readonly Subject<T> onClickCancel = new Subject<T>();

        private int _originalCount;

        public ItemCountPopup()
        {
            item.Subscribe(value =>
            {
                if (ReferenceEquals(value, null))
                {
                    _originalCount = 0;
                    return;
                }
                
                _originalCount = value.count.Value;
            });
            
            onClickMinus.Subscribe(value =>
            {
                if (ReferenceEquals(value, null) ||
                    value.item.Value.count.Value <= item.Value.minCount.Value)
                {
                    return;
                }

                value.item.Value.count.Value--;
            });
            
            onClickPlus.Subscribe(value =>
            {
                if (ReferenceEquals(value, null) ||
                    value.item.Value.count.Value >= item.Value.maxCount.Value)
                {
                    return;
                }

                value.item.Value.count.Value++;
            });

            onClickCancel.Subscribe(value => value.item.Value.count.Value = _originalCount);
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
            onClickCancel.Dispose();
        }
    }
}
