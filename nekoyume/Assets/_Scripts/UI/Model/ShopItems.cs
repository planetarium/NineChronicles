using System;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class ShopItems : IDisposable
    {
        public readonly ShopItemReactiveCollection buyItems = new ShopItemReactiveCollection();
        public readonly ShopItemReactiveCollection sellItems = new ShopItemReactiveCollection();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        public ShopItems(Game.Shop shop)
        {
//            ResetBuyItems(shop);
        }
        
        public void Dispose()
        {
            buyItems.DisposeAll();
            sellItems.DisposeAll();
            
            onClickRefresh.Dispose();
        }

        public void ResetBuyItems(Game.Shop shop)
        {
            var index = UnityEngine.Random.Range(0, shop.items.Count);

            for (var i = 0; i < 16; i++)
            {
                var keyValuePair = shop.items.ElementAt(index);
                if (keyValuePair.Value.Count == 0)
                {
                    i--;
                    continue;
                }

                var item = keyValuePair.Value.ElementAt(1);
                
//                buyItems.Add(new ShopItem(item));

                index++;
                if (index == shop.items.Count)
                {
                    index = 0;
                }
            }
        }
    }
}
