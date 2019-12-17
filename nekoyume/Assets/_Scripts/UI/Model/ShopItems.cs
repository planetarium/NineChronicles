using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveProperty<UI.Shop.StateType> State = new ReactiveProperty<UI.Shop.StateType>();
        public readonly ReactiveCollection<ShopItem> CurrentAgentsProducts = new ReactiveCollection<ShopItem>();
        public readonly ReactiveCollection<ShopItem> OtherProducts = new ReactiveCollection<ShopItem>();

        public readonly ReactiveProperty<ShopItemView> SelectedItemView = new ReactiveProperty<ShopItemView>();

        public readonly ReactiveProperty<ShopItem> SelectedItemViewModel =
            new ReactiveProperty<ShopItem>();

        public readonly Subject<ShopItemView> OnDoubleClickItemView = new Subject<ShopItemView>();

        private IDictionary<Address, List<Game.Item.ShopItem>> _shopItems;

        public ShopItems(IDictionary<Address, List<Game.Item.ShopItem>> shopItems = null)
        {
            CurrentAgentsProducts.ObserveRemove().Subscribe(SubscribeProductRemove);
            OtherProducts.ObserveRemove().Subscribe(SubscribeProductRemove);

            ResetProducts(shopItems);
        }

        public void Dispose()
        {
            State.Dispose();
            CurrentAgentsProducts.DisposeAllAndClear();
            OtherProducts.DisposeAllAndClear();
            SelectedItemView.Dispose();
            SelectedItemViewModel.Dispose();
            OnDoubleClickItemView.Dispose();
        }

        public void ResetProducts(IDictionary<Address, List<Game.Item.ShopItem>> shopItems)
        {
            _shopItems = shopItems ?? new Dictionary<Address, List<Game.Item.ShopItem>>();

            ResetCurrentAgentsProducts();
            ResetOtherProducts();
        }

        private void SubscribeItemOnClick(ShopItemView view)
        {
            if (view is null ||
                view == SelectedItemView.Value)
            {
                DeselectItemView();
                return;
            }

            SelectItemView(view);
        }

        public void SelectItemView(ShopItemView view)
        {
            if (view is null ||
                view.Model is null)
                return;

            DeselectItemView();

            SelectedItemView.Value = view;
            SelectedItemViewModel.Value = view.Model;
            SelectedItemViewModel.Value.Selected.Value = true;
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null ||
                SelectedItemViewModel.Value is null)
            {
                return;
            }

            SelectedItemViewModel.Value.Selected.Value = false;
            SelectedItemViewModel.Value = null;
            SelectedItemView.Value = null;
        }

        #region Shop Item

        public void AddProduct(Address sellerAgentAddress, Game.Item.ShopItem shopItem)
        {
            if (!_shopItems.ContainsKey(sellerAgentAddress))
            {
                _shopItems.Add(sellerAgentAddress, new List<Game.Item.ShopItem>());
            }

            _shopItems[sellerAgentAddress].Add(shopItem);
        }

        public void RemoveProduct(Address sellerAgentAddress, Guid productId)
        {
            if (!_shopItems.ContainsKey(sellerAgentAddress))
            {
                return;
            }

            foreach (var shopItem in _shopItems[sellerAgentAddress])
            {
                if (shopItem.ProductId != productId)
                {
                    continue;
                }

                _shopItems[sellerAgentAddress].Remove(shopItem);
                break;
            }
        }

        public ShopItem AddCurrentAgentsProduct(Address sellerAgentAddress, Game.Item.ShopItem shopItem)
        {
            var result = CreateShopItem(sellerAgentAddress, shopItem);
            CurrentAgentsProducts.Add(result);
            return result;
        }

        public void RemoveCurrentAgentsProduct(Guid productId)
        {
            RemoveProduct(CurrentAgentsProducts, productId);
        }

        public void RemoveOtherProduct(Guid productId)
        {
            RemoveProduct(OtherProducts, productId);
        }

        private static void RemoveProduct(ICollection<ShopItem> collection, Guid productId)
        {
            var shouldRemove = collection.FirstOrDefault(item => item.ProductId.Value == productId);
            if (!(shouldRemove is null))
            {
                collection.Remove(shouldRemove);
            }
        }

        private void SubscribeProductRemove(CollectionRemoveEvent<ShopItem> e)
        {
            // 데이터의 프로퍼티를 외부에서 처분하는 부분 기억.
            e.Value.OnClick.Dispose();
        }

        #endregion

        #region Reset

        public void ResetOtherProducts()
        {
            OtherProducts.Clear();

            if (_shopItems.Count == 0)
                return;

            var startIndex = UnityEngine.Random.Range(0, _shopItems.Count);
            var index = startIndex;
            var total = 16;

            for (var i = 0; i < total; i++)
            {
                var keyValuePair = _shopItems.ElementAt(index);
                if (keyValuePair.Value.Count == 0)
                    continue;

                foreach (var shopItem in keyValuePair.Value)
                {
                    if (keyValuePair.Key.Equals(States.Instance.AgentState.address))
                        continue;

                    OtherProducts.Add(CreateShopItem(keyValuePair.Key, shopItem));
                    if (OtherProducts.Count == total)
                        return;
                }

                index = index + 1 == _shopItems.Count ? 0 : index + 1;

                if (index == startIndex)
                    break;
            }
        }

        public void ResetCurrentAgentsProducts()
        {
            CurrentAgentsProducts.Clear();

            if (_shopItems.Count == 0)
                return;

            var sellerAgentAddress = States.Instance.AgentState.address;
            if (!_shopItems.ContainsKey(sellerAgentAddress))
                return;

            var shopItems = _shopItems[sellerAgentAddress];
            foreach (var shopItem in shopItems)
            {
                CurrentAgentsProducts.Add(CreateShopItem(sellerAgentAddress, shopItem));
            }
        }

        private ShopItem CreateShopItem(Address key, Game.Item.ShopItem shopItem)
        {
            var item = new ShopItem(key, shopItem);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                    return;

                SubscribeItemOnClick(shopItemViewModel.View);
            });
            item.OnDoubleClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                    return;

                DeselectItemView();
                OnDoubleClickItemView.OnNext(shopItemViewModel.View);
            });

            return item;
        }

        #endregion
    }
}
