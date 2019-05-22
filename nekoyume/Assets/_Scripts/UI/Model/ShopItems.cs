using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> products = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> registeredProducts = new ReactiveCollection<ShopItem>();
        public readonly ReactiveProperty<ShopItem> selectedItem = new ReactiveProperty<ShopItem>();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        private Game.Shop _shop;

        public ShopItems(Game.Shop shop)
        {
            if (ReferenceEquals(shop, null))
            {
                throw new ArgumentNullException();
            }

            _shop = shop;

            products.ObserveAdd().Subscribe(OnAddShopItem);
            products.ObserveRemove().Subscribe(OnRemoveShopItem);
            registeredProducts.ObserveAdd().Subscribe(OnAddShopItem);
            registeredProducts.ObserveRemove().Subscribe(OnRemoveShopItem);
            onClickRefresh.Subscribe(_ => ResetBuyItems());
            
            ResetBuyItems();
            ResetSellItems();
        }
        
        public void Dispose()
        {
            products.DisposeAll();
            registeredProducts.DisposeAll();
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

        public void RemoveBuyItem(Guid productId, int count)
        {
            RemoveItem(products, productId, count);
        }
        
        public void RemoveSellItem(Guid productId, int count)
        {
            RemoveItem(registeredProducts, productId, count);
        }
        
        private void RemoveItem(ICollection<ShopItem> collection, Guid productId, int count)
        {
            ShopItem shouldRemove = null;
            foreach (var item in collection)
            {
                if (item.productId.Value != productId)
                {
                    continue;
                }
                
                if (item.count.Value > count)
                {
                    item.count.Value -= count;
                }
                else if (item.count.Value == count)
                {
                    shouldRemove = item;
                }
                else
                {
                    throw new InvalidOperationException($"item({productId}) count is lesser then {count}");
                }
                
                break;
            }

            if (!ReferenceEquals(shouldRemove, null))
            {
                collection.Remove(shouldRemove);
            }
        }

        private void OnAddShopItem(CollectionAddEvent<ShopItem> e)
        {
            e.Value.onClick.Subscribe(OnClickShopItem);
        }
        
        private void OnRemoveShopItem(CollectionRemoveEvent<ShopItem> e)
        {
            // 데이터의 프로퍼티를 외부에서 처분하는 부분 기억.
            e.Value.onClick.Dispose();
        }
        
        private void OnClickShopItem(ShopItem shopItem)
        {
            if (!ReferenceEquals(selectedItem.Value, null))
            {
                selectedItem.Value.selected.Value = false;
                
                if (selectedItem.Value.productId == shopItem.productId)
                {
                    selectedItem.Value = null;
                    return;
                }
            }

            selectedItem.Value = shopItem;
            selectedItem.Value.selected.Value = true;
        }

        private void ResetBuyItems()
        {
            products.Clear();
            
            var startIndex = UnityEngine.Random.Range(0, _shop.items.Count);
            var index = startIndex;
            var total = 16;

            for (var i = 0; i < total; i++)
            {
                var keyValuePair = _shop.items.ElementAt(index);
                var count = keyValuePair.Value.Count;
                if (count == 0)
                {
                    continue;
                }

                foreach (var shopItem in keyValuePair.Value)
                {
                    products.Add(new ShopItem(keyValuePair.Key, shopItem));
                    if (products.Count == total)
                    {
                        return;
                    }
                }
                
                if (index + 1 == _shop.items.Count)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
                
                if (index == startIndex)
                {
                    break;
                }
            }
        }

        private void ResetSellItems()
        {
            registeredProducts.Clear();
            
            var key = ActionManager.instance.AvatarAddress;
            if (!_shop.items.ContainsKey(key))
            {
                return;
            }

            var items = _shop.items[key];
            foreach (var item in items)
            {
                registeredProducts.Add(new ShopItem(key, item));
            }
        }
    }
}
