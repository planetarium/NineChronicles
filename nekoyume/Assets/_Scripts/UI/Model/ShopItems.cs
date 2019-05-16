using System;
using System.Linq;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> buyItems = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> sellItems = new ReactiveCollection<ShopItem>();
        public readonly ReactiveProperty<ShopItem> selectedItem = new ReactiveProperty<ShopItem>();
        
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

            buyItems.ObserveAdd().Subscribe(OnAddShopItem);
            sellItems.ObserveAdd().Subscribe(OnAddShopItem);
            
            ResetBuyItems(shop);
            ResetSellItems(shop);
        }
        
        public void Dispose()
        {
            buyItems.DisposeAll();
            sellItems.DisposeAll();
            selectedItem.DisposeAll();
            
            onClickRefresh.Dispose();
        }

        public void DeselectAll()
        {
            if (ReferenceEquals(selectedItem.Value, null))
            {
                return;
            }

            selectedItem.Value.selected.Value = false;
            selectedItem.Value = null;
        }

        private void OnAddShopItem(CollectionAddEvent<ShopItem> e)
        {
            e.Value.selected.Subscribe(_ => selectedItem.Value = e.Value);
        }

        private void ResetBuyItems(Game.Shop shop)
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
                    buyItems.Add(new ShopItem(keyValuePair.Key, shopItem));
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

        private void ResetSellItems(Game.Shop shop)
        {
            var key = ActionManager.instance.AvatarAddress.ToString();
            if (!shop.items.ContainsKey(key))
            {
                return;
            }

            var items = shop.items[key];
            foreach (var item in items)
            {
                sellItems.Add(new ShopItem(key, item));
            }
        }
    }
}
