using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInfo : IDisposable
    {
        public readonly ReactiveProperty<InventoryItem> item = new ReactiveProperty<InventoryItem>(null);
        public readonly ReactiveProperty<bool> buttonEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<string> buttonText = new ReactiveProperty<string>("");
        
        public readonly Subject<ItemInfo> onClick = new Subject<ItemInfo>();

        public void Dispose()
        {
            item.DisposeAll();
            buttonEnabled.Dispose();
            buttonText.Dispose();
            
            onClick.Dispose();
        }
    }
}
