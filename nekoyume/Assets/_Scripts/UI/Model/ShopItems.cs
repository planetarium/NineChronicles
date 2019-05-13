using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> buyItems = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> sellItems = new ReactiveCollection<ShopItem>();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        public ShopItems()
        {
            
        }

        public void Dispose()
        {
            buyItems.DisposeAll();
            sellItems.DisposeAll();
            
            onClickRefresh.Dispose();
        }
    }
}
