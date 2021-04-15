using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
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

        public ItemSubTypeFilter itemSubTypeFilter = ItemSubTypeFilter.Weapon;
        public SortFilter sortFilter = SortFilter.Class;
        public List<int> searchIds = new List<int>();
        public bool isReverseOrder = false;
        public bool isMultiplePurchase = false;

        private IReadOnlyDictionary<
            Address, Dictionary<
                ItemSubTypeFilter, Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>>
            _agentProducts;

        private IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>
            _itemSubTypeProducts;

        public readonly List<ShopItem> wishItems = new List<ShopItem>();

        public readonly int WishListSize = 8;

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
                var wishItem = wishItems.FirstOrDefault(x =>
                    x.ProductId.Value == view.Model.ProductId.Value);
                if (wishItem is null)
                {
                    if (wishItems.Count < WishListSize)
                    {
                        wishItems.Add(view.Model);
                        SelectedItemView.SetValueAndForceNotify(view);
                        SelectedItemViewModel.SetValueAndForceNotify(view.Model);
                        SelectedItemViewModel.Value.Selected.SetValueAndForceNotify(true);
                    }
                    else
                    {
                        OneLinePopup.Push(MailType.System,
                            L10nManager.Localize("NOTIFICATION_BUY_WISHLIST_FULL"));
                    }
                }
                else
                {
                    SelectedItemViewModel.SetValueAndForceNotify(view.Model);
                    SelectedItemViewModel.Value.Selected.SetValueAndForceNotify(false);
                    SelectedItemView.SetValueAndForceNotify(view);
                    wishItems.Remove(wishItem);

                    SelectedItemViewModel.SetValueAndForceNotify(null);
                    SelectedItemView.SetValueAndForceNotify(null);
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
            ResetSelectedState();
        }

        public void SetMultiplePurchase(bool value)
        {
            ClearWishList();
            isMultiplePurchase = value;
        }

        public void SelectItemView(ShopItemView view)
        {
            if (view is null ||
                view.Model is null)
                return;

            DeselectItemView();
            SelectedItemView.SetValueAndForceNotify(view);
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
            SelectedItemView.SetValueAndForceNotify(null);
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
            foreach (var keyValuePair in _agentProducts)
            {
                foreach (var keyValuePair1 in keyValuePair.Value
                    .SelectMany(valuePair => valuePair.Value.SelectMany(pair => pair.Value)))
                {
                    foreach (var shopItem in keyValuePair1.Value)
                    {
                        if (productId == shopItem.ProductId.Value)
                        {
                            keyValuePair1.Value.Remove(shopItem);
                            break;
                        }
                    }
                }
            }

            foreach (var itemSubTypeProduct in _itemSubTypeProducts)
            {
                foreach (var valuePair in itemSubTypeProduct.Value
                    .SelectMany(keyValuePair => keyValuePair.Value))
                {
                    foreach (var shopItem in valuePair.Value)
                    {
                        if (productId == shopItem.ProductId.Value)
                        {
                            valuePair.Value.Remove(shopItem);
                            break;
                        }
                    }
                }
            }

            ResetAgentProducts();
            ResetItemSubTypeProducts();
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
            if (States.Instance == null || States.Instance.AgentState == null)
            {
                return;
            }

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
            ResetSelectedState();
        }

        private void ResetSelectedState()
        {
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
