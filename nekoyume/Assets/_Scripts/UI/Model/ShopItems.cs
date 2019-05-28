using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> products = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> registeredProducts = new ReactiveCollection<ShopItem>();
        public readonly ReactiveProperty<ShopItem> selectedItem = new ReactiveProperty<ShopItem>();
        
        public readonly Subject<ShopItems> onClickRefresh = new Subject<ShopItems>();

        private readonly IDictionary<Address, List<Game.Item.ShopItem>> _shopItems;

        public ShopItems(IDictionary<Address, List<Game.Item.ShopItem>> shopItems)
        {
            if (ReferenceEquals(shopItems, null))
            {
                throw new ArgumentNullException();
            }

            _shopItems = shopItems;

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

        public void RemoveProduct(Guid productId)
        {
            RemoveItem(products, productId);
        }

        public ShopItem AddRegisteredProduct(Address sellerAvatarAddress, Game.Item.ShopItem shopItem)
        {
            var result = new ShopItem(sellerAvatarAddress, shopItem);
            registeredProducts.Add(result);
            return result;
        }
        
        public void RemoveRegisteredProduct(Guid productId)
        {
            RemoveItem(registeredProducts, productId);
        }
        
        private void RemoveItem(ICollection<ShopItem> collection, Guid productId)
        {
            var shouldRemove = collection.FirstOrDefault(item => item.productId.Value == productId);

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
        
        public void OnClickShopItem(ShopItem shopItem)
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

            if (_shopItems.Count == 0)
            {
                return;
            }
            
            var startIndex = UnityEngine.Random.Range(0, _shopItems.Count);
            var index = startIndex;
            var total = 16;

            for (var i = 0; i < total; i++)
            {
                var keyValuePair = _shopItems.ElementAt(index);
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
                
                if (index + 1 == _shopItems.Count)
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
            
            if (_shopItems.Count == 0)
            {
                return;
            }

            var key = States.CurrentAvatar.Value.address;
            if (!_shopItems.ContainsKey(key))
            {
                return;
            }

            var items = _shopItems[key];
            foreach (var item in items)
            {
                registeredProducts.Add(new ShopItem(key, item));
            }
        }
    }
}
