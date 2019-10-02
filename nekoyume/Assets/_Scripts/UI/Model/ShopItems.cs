using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveCollection<ShopItem> Products = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> RegisteredProducts = new ReactiveCollection<ShopItem>();
        public readonly ReactiveProperty<ShopItemView> SelectedItemView = new ReactiveProperty<ShopItemView>();
        
        public readonly Subject<ShopItems> OnRefresh = new Subject<ShopItems>();

        private IDictionary<Address, List<Game.Item.ShopItem>> _shopItems;
        
        public ShopItems(IDictionary<Address, List<Game.Item.ShopItem>> shopItems = null)
        {
            Products.ObserveAdd().Subscribe(OnAddShopItem);
            Products.ObserveRemove().Subscribe(OnRemoveShopItem);
            RegisteredProducts.ObserveAdd().Subscribe(OnAddShopItem);
            RegisteredProducts.ObserveRemove().Subscribe(OnRemoveShopItem);
            OnRefresh.Subscribe(_ => ResetBuyItems());
            
            ResetItems(shopItems);
        }
        
        public void Dispose()
        {
            Products.DisposeAllAndClear();
            RegisteredProducts.DisposeAllAndClear();
            SelectedItemView.Dispose();
            
            OnRefresh.Dispose();
        }

        public void ResetItems(IDictionary<Address, List<Game.Item.ShopItem>> shopItems)
        {
            _shopItems = shopItems ?? new Dictionary<Address, List<Game.Item.ShopItem>>();
            
            ResetBuyItems();
            ResetSellItems();
        }
        
        private void SubscribeItemOnClick(ShopItemView view)
        {
            if (view is null ||
                view == SelectedItemView.Value)
            {
                DeselectItemView();

                return;
            }

            DeselectItemView();
            SelectItemView(view);

//            if (SelectedItemView.Value is null ||
//                SelectedItemView.Value != view)
//            {
//                SelectedItemView.SetValueAndForceNotify(view);
//                SelectedItemView.Value.Model.Selected.Value = true;
//            }
        }

        public void SelectItemView(ShopItemView view)
        {
            if (view is null ||
                view.Model is null)
            {
                return;
            }
            
            SelectedItemView.Value = view;
            SelectedItemView.Value.Model.Selected.Value = true;
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null ||
                SelectedItemView.Value.Model is null)
            {
                return;
            }

            SelectedItemView.Value.Model.Selected.Value = false;
            SelectedItemView.Value = null;
        }

        #region Shop Item

        public void AddShopItem(Address sellerAgentAddress, Game.Item.ShopItem shopItem)
        {
            if (!_shopItems.ContainsKey(sellerAgentAddress))
            {
                _shopItems.Add(sellerAgentAddress, new List<Game.Item.ShopItem>());
            }
            
            _shopItems[sellerAgentAddress].Add(shopItem);
        }
        
        public void RemoveShopItem(Address sellerAgentAddress, Guid productId)
        {
            if (!_shopItems.ContainsKey(sellerAgentAddress))
            {
                return;
            }

            foreach (var shopItem in _shopItems[sellerAgentAddress])
            {
                if (shopItem.productId != productId)
                {
                    continue;
                }
                
                _shopItems[sellerAgentAddress].Remove(shopItem);
                break;
            }
        }

        #endregion

        public ShopItem AddRegisteredProduct(Address sellerAgentAddress, Game.Item.ShopItem shopItem)
        {
            var result = new ShopItem(sellerAgentAddress, shopItem);
            RegisteredProducts.Add(result);
            return result;
        }
        
        public void RemoveProduct(Guid productId)
        {
            RemoveItem(Products, productId);
        }
        
        public void RemoveRegisteredProduct(Guid productId)
        {
            RemoveItem(RegisteredProducts, productId);
        }
        
        private void RemoveItem(ICollection<ShopItem> collection, Guid productId)
        {
            var shouldRemove = collection.FirstOrDefault(item => item.ProductId.Value == productId);

            if (!ReferenceEquals(shouldRemove, null))
            {
                collection.Remove(shouldRemove);
            }
        }

        private void OnAddShopItem(CollectionAddEvent<ShopItem> e)
        {
            e.Value.OnClick.Subscribe(SubscribeItemOnClick);
        }
        
        private void OnRemoveShopItem(CollectionRemoveEvent<ShopItem> e)
        {
            // 데이터의 프로퍼티를 외부에서 처분하는 부분 기억.
            e.Value.OnClick.Dispose();
        }

        #region Reset
        
        private void ResetBuyItems()
        {
            Products.Clear();

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
                    if (keyValuePair.Key.Equals(States.Instance.agentState.Value.address))
                    {
                        continue;
                    }
                    Products.Add(new ShopItem(keyValuePair.Key, shopItem));
                    if (Products.Count == total)
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
            RegisteredProducts.Clear();
            
            if (_shopItems.Count == 0)
            {
                return;
            }

            var key = States.Instance.agentState.Value.address;
            if (!_shopItems.ContainsKey(key))
            {
                return;
            }

            var items = _shopItems[key];
            foreach (var item in items)
            {
                RegisteredProducts.Add(new ShopItem(key, item));
            }
        }
        
        #endregion
    }
}
