using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> items = new ReactiveCollection<ShopItem>();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        public ShopItems()
        {
            
        }

        public void Dispose()
        {
            items.DisposeAll();
            
            onClickRefresh.Dispose();
        }
    }
}
