using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ShopItems : IDisposable
    {
        public readonly ReactiveProperty<UI.Shop.StateType> State =
            new ReactiveProperty<UI.Shop.StateType>();

        public readonly ReactiveProperty<Dictionary<int, List<ShopItem>>> AgentProducts =
            new ReactiveProperty<Dictionary<int, List<ShopItem>>>();

        public readonly ReactiveProperty<Dictionary<int, List<ShopItem>>> ItemSubTypeProducts
            = new ReactiveProperty<Dictionary<int, List<ShopItem>>>();

        public readonly ReactiveProperty<ShopItemView> SelectedItemView =
            new ReactiveProperty<ShopItemView>();

        public readonly ReactiveProperty<ShopItem> SelectedItemViewModel =
            new ReactiveProperty<ShopItem>();

        public readonly Subject<ShopItemView> OnDoubleClickItemView = new Subject<ShopItemView>();

        public ItemSubTypeFilter itemSubTypeFilter = ItemSubTypeFilter.All;
        public SortFilter sortFilter = SortFilter.Class;
        public List<int> searchIds = new List<int>();
        public bool isReverseOrder = false;
        public bool isMultiplePurchase = true;

        private IReadOnlyDictionary<
            Address, Dictionary<
                ItemSubTypeFilter, Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>>
            _agentProducts;

        private IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>
            _itemSubTypeProducts;

        public readonly List<ShopItem> wishItems = new List<ShopItem>();

        public void Dispose()
        {
            State.Dispose();
            AgentProducts.Dispose();
            ItemSubTypeProducts.Dispose();
            SelectedItemView.Dispose();
            SelectedItemViewModel.Dispose();
            OnDoubleClickItemView.Dispose();
        }

        public void ResetAgentProducts(IReadOnlyDictionary<
            Address, Dictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<
                        int, List<Nekoyume.Model.Item.ShopItem>>>>> products)
        {
            _agentProducts = products is null
                ? new Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter, Dictionary<
                            SortFilter, Dictionary<int, List<ShopItem>>>>>()
                : products.ToDictionary(
                    pair => pair.Key,
                    pair => ModelToViewModel(pair.Value));

            ResetAgentProducts();
        }

        public void ResetItemSubTypeProducts(IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<Nekoyume.Model.Item.ShopItem>>
                >>
            products)
        {
            _itemSubTypeProducts = products is null
                ? new Dictionary<
                    ItemSubTypeFilter, Dictionary<
                        SortFilter, Dictionary<int, List<ShopItem>>>>()
                : ModelToViewModel(products);


            ResetItemSubTypeProducts();
        }

        private Dictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>>
            ModelToViewModel(IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<
                        int, List<Nekoyume.Model.Item.ShopItem>>>> shopItems)
        {
            return shopItems.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToDictionary(
                    pair2 => pair2.Key,
                    pair2 => pair2.Value.ToDictionary(
                        pair3 => pair3.Key,
                        pair3 => pair3.Value.Select(CreateShopItem).ToList())));
        }

        private void SubscribeItemOnClick(ShopItemView view)
        {
            if (isMultiplePurchase)
            {
                var selected = wishItems.FirstOrDefault(x =>
                    x.ProductId.Value == view.Model.ProductId.Value);
                if (selected is null)
                {
                    Debug.Log("wishlist add item");
                    wishItems.Add(view.Model);
                    SelectedItemView.Value = view;
                    SelectedItemViewModel.Value = view.Model;
                    SelectedItemViewModel.Value.Selected.Value = true;
                }
                else
                {
                    Debug.Log("wishlist remove item");
                    SelectedItemViewModel.Value = view.Model;
                    SelectedItemViewModel.Value.Selected.Value = false;
                    SelectedItemView.Value = view;
                    wishItems.Remove(selected);

                    SelectedItemViewModel.Value = null;
                    SelectedItemView.Value = null;
                }
            }
            else
            {
                if (view is null || view == SelectedItemView.Value)
                {
                    DeselectItemView();
                    return;
                }

                SelectItemView(view);
            }
        }

        public void RemoveItemInWishList(ShopItem shopItem)
        {
            var selected = wishItems.FirstOrDefault(x =>
                x.ProductId.Value == shopItem.ProductId.Value);

            if (selected is null)
            {
                return;
            }

            wishItems.Remove(shopItem);
            foreach (var keyValuePair in ItemSubTypeProducts.Value)
            {
                var reuslt = keyValuePair.Value.FirstOrDefault(
                    x => x.ProductId.Value == selected.ProductId.Value);
                if (reuslt != null)
                {
                    SelectedItemViewModel.Value = reuslt;
                    SelectedItemViewModel.Value.Selected.Value = false;
                    SelectedItemView.Value = reuslt.View;

                    SelectedItemViewModel.Value = null;
                    SelectedItemView.Value = null;
                    return;
                }
            }
        }

        public void ClearWishList()
        {
            wishItems.Clear();
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

        public void RemoveAgentProduct(Guid productId)
        {
            var agentAddress = States.Instance.AgentState.address;
            if (!_agentProducts.ContainsKey(agentAddress))
            {
                return;
            }

            RemoveProduct(productId, _agentProducts[agentAddress], AgentProducts.Value);
            AgentProducts.SetValueAndForceNotify(AgentProducts.Value);
        }

        public void RemoveItemSubTypeProduct(Guid productId)
        {
            RemoveProduct(productId, _itemSubTypeProducts, ItemSubTypeProducts.Value);
            ItemSubTypeProducts.SetValueAndForceNotify(ItemSubTypeProducts.Value);
        }

        private static void RemoveProduct(
            Guid productId,
            IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>> origin,
            Dictionary<int, List<ShopItem>> reactivePropertyValue)
        {
            foreach (var pair in origin)
            {
                var removed = false;
                foreach (var pair2 in pair.Value)
                {
                    foreach (var pair3 in pair2.Value)
                    {
                        var target = pair3.Value.FirstOrDefault(item =>
                            item.ProductId.Value.Equals(productId));
                        if (target is null)
                        {
                            continue;
                        }

                        target.Dispose();
                        pair3.Value.Remove(target);
                        removed = true;
                    }
                }

                if (removed)
                {
                    break;
                }
            }

            foreach (var pair in reactivePropertyValue)
            {
                var target = pair.Value.FirstOrDefault(item =>
                    item.ProductId.Value.Equals(productId));
                if (target is null)
                {
                    continue;
                }

                target.Dispose();
                pair.Value.Remove(target);
            }
        }

        #endregion

        #region Reset

        public void ResetAgentProducts()
        {
            var agentAddress = States.Instance.AgentState.address;
            if (_agentProducts is null ||
                !_agentProducts.ContainsKey(agentAddress))
            {
                AgentProducts.Value = new Dictionary<int, List<ShopItem>>();
                return;
            }

            AgentProducts.Value = GetFilteredAndSortedProducts(_agentProducts[agentAddress]);
        }

        public void ResetItemSubTypeProducts()
        {
            ItemSubTypeProducts.Value = GetFilteredAndSortedProducts(_itemSubTypeProducts);
            foreach (var keyValuePair in ItemSubTypeProducts.Value)
            {
                foreach (var shopItem in keyValuePair.Value)
                {
                    var isSelected =
                        wishItems.Exists(x => x.ProductId.Value == shopItem.ProductId.Value);
                    shopItem.Selected.Value = isSelected;
                }
            }
        }

        private Dictionary<int, List<ShopItem>> GetFilteredAndSortedProducts(IReadOnlyDictionary<
            ItemSubTypeFilter, Dictionary<
                SortFilter, Dictionary<int, List<ShopItem>>>> products)
        {
            if (products is null)
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            if (!products.ContainsKey(itemSubTypeFilter))
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            var itemSubTypeProducts = products[itemSubTypeFilter];
            if (!itemSubTypeProducts.ContainsKey(sortFilter))
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            var sortProducts = itemSubTypeProducts[sortFilter];
            if (sortProducts.Count == 0)
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            var shopItems = new List<ShopItem>();
            foreach (var product in sortProducts)
            {
                if (searchIds.Count > 0) //search
                {
                    var select = product.Value
                        .Where(x => searchIds.Exists(y => y == x.ItemBase.Value.Id));
                    shopItems.AddRange(select);
                }
                else
                {
                    shopItems.AddRange(product.Value);
                }
            }

            if (shopItems.Count == 0)
            {
                return new Dictionary<int, List<ShopItem>>();
            }

            if (isReverseOrder)
            {
                shopItems.Reverse();
            }

            var result = new Dictionary<int, List<ShopItem>>();
            int setCount = sortProducts.First().Value.Count;
            int index = 0;
            int page = 0;
            while (true)
            {
                var count = Math.Min(shopItems.Count - index, setCount);
                if (count <= 0)
                {
                    break;
                }

                var items = shopItems.GetRange(index, count);
                result.Add(page, items);
                index += count;
                page ++;
            }

            return result;
        }

        private ShopItem CreateShopItem(Nekoyume.Model.Item.ShopItem shopItem)
        {
            var item = new ShopItem(shopItem);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                {
                    return;
                }

                SubscribeItemOnClick(shopItemViewModel.View);
            });
            item.OnDoubleClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                {
                    return;
                }

                DeselectItemView();
                OnDoubleClickItemView.OnNext(shopItemViewModel.View);
            });

            return item;
        }

        #endregion

        public bool TryGetShopItemFromAgentProducts(Guid itemId, out ShopItem shopItem)
        {
            shopItem = AgentProducts.Value.Values
                .SelectMany(list => list)
                .Where(item => item.ItemBase.Value is INonFungibleItem)
                .FirstOrDefault(item =>
                    ((INonFungibleItem) item.ItemBase.Value).ItemId.Equals(itemId));

            return !(shopItem is null);
        }

        public bool TryGetShopItemFromItemSubTypeProducts(Guid itemId, out ShopItem shopItem)
        {
            shopItem = ItemSubTypeProducts.Value.Values
                .SelectMany(list => list)
                .Where(item => item.ItemBase.Value is INonFungibleItem)
                .FirstOrDefault(item =>
                    ((INonFungibleItem) item.ItemBase.Value).ItemId.Equals(itemId));

            return !(shopItem is null);
        }
    }
}
