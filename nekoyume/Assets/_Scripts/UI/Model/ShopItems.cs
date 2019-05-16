using System;
using System.Linq;
using Nekoyume.Action;
using UniRx;
using Unity.Mathematics;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> buyItems = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> sellItems = new ReactiveCollection<ShopItem>();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        public ShopItems(Game.Shop shop)
        {
            if (ReferenceEquals(shop, null))
            {
                throw new ArgumentNullException();
            }
            
            if (shop.items.Count == 0)
            {
                return;
            }
            
            ResetBuyItems(shop);
            ResetSellItems(shop);
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
            var total = 16;

            while (true)
            {
                var keyValuePair = shop.items.ElementAt(index);
                var count = keyValuePair.Value.Count;
                if (count == 0)
                {
                    continue;
                }

                foreach (var shopItem in keyValuePair.Value)
                {
                    buyItems.Add(new ShopItem(shopItem));
                    total--;
                    if (total == 0)
                    {
                        return;
                    }
                }

                index++;
                if (index == shop.items.Count)
                {
                    break;
                }
            }
        }

        public void ResetSellItems(Game.Shop shop)
        {
            var key = ActionManager.instance.agentAddress.ToByteArray();
            if (!shop.items.ContainsKey(key))
            {
                return;
            }

            var items = shop.items[key];
            foreach (var item in items)
            {
                sellItems.Add(new ShopItem(item));
            }
        }
    }
}
