using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInfo : IDisposable
    {
        public readonly ReactiveProperty<Inventory.Item> Item = new ReactiveProperty<Inventory.Item>(null);
        public readonly ReactiveProperty<bool> ButtonEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<string> ButtonText = new ReactiveProperty<string>("");
        
        public readonly Subject<ItemInfo> OnClick = new Subject<ItemInfo>();

        public void Dispose()
        {
            Item.DisposeAll();
            ButtonEnabled.Dispose();
            ButtonText.Dispose();
            
            OnClick.Dispose();
        }
    }
}
