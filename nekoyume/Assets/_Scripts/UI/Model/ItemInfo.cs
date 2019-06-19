using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInfo : IDisposable
    {
        public readonly ReactiveProperty<InventoryItem> item = new ReactiveProperty<InventoryItem>(null);
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>(0);
        public readonly ReactiveProperty<bool> priceEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> buttonEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<string> buttonText = new ReactiveProperty<string>("");

        public readonly ReactiveProperty<Func<InventoryItem, bool>> buttonEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly Subject<InventoryItem> onClick = new Subject<InventoryItem>();

        private readonly List<IDisposable> _disposablesForItem = new List<IDisposable>();

        public ItemInfo()
        {
            buttonEnabledFunc.Value = ButtonEnabledFunc;

            item.Subscribe(item =>
            {
                _disposablesForItem.DisposeAllAndClear();

                if (item == null)
                {
                    buttonEnabled.Value = false;
                    return;
                }

                buttonEnabled.Value = buttonEnabledFunc.Value(item);
            });

            buttonEnabledFunc.Subscribe(func =>
            {
                if (func == null)
                {
                    buttonEnabledFunc.Value = ButtonEnabledFunc;
                }

                buttonEnabled.Value = buttonEnabledFunc.Value(item.Value);
            });
        }

        public void Dispose()
        {
            item.DisposeAll();
            price.Dispose();
            priceEnabled.Dispose();
            buttonEnabled.Dispose();
            buttonText.Dispose();

            onClick.Dispose();
        }

        private static bool ButtonEnabledFunc(InventoryItem inventoryItem)
        {
            if (ReferenceEquals(inventoryItem, null))
            {
                return false;
            }
            
            return !inventoryItem.dimmed.Value;
        }
    }
}
