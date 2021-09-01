using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Module;

namespace Nekoyume.UI.Model
{
    using UniRx;

    public abstract class ShopItems : IDisposable
    {
        public readonly ReactiveProperty<Dictionary<int, List<ShopItem>>> Items
            = new ReactiveProperty<Dictionary<int, List<ShopItem>>>();
        public readonly ReactiveProperty<ShopItemView> SelectedItemView =
            new ReactiveProperty<ShopItemView>();
        protected readonly ReactiveProperty<ShopItem> _selectedItemViewModel =
            new ReactiveProperty<ShopItem>();
        private readonly Subject<ShopItemView> _onDoubleClickItemView = new Subject<ShopItemView>();

        public ItemSubTypeFilter itemSubTypeFilter = ItemSubTypeFilter.Weapon;
        public ShopSortFilter sortFilter = ShopSortFilter.Class;
        public List<int> searchIds = new List<int>();
        public bool isReverseOrder = false;
        public bool isMultiplePurchase = false;

        private IReadOnlyDictionary<ItemSubTypeFilter,
                Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>> _items;

        protected abstract void OnClickItem(ShopItemView view);
        protected abstract void ResetSelectedState();

        public void Dispose()
        {
            Items.Dispose();
            SelectedItemView.Dispose();
            _selectedItemViewModel.Dispose();
            _onDoubleClickItemView.Dispose();
        }

        // reactive shop state에서 스테이트 가져올때 전체적으로 리셋해줌.
        public void ResetItems(IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>> digests)
        {
            _items = digests is null
                ? new Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>()
                : DigestToViewModel(digests);
            ResetShopItems();
        }

        private Dictionary<ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>
            DigestToViewModel(IReadOnlyDictionary<ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<OrderDigest>>>> shopItems)
        {
            return shopItems.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToDictionary(
                    pair2 => pair2.Key,
                    pair2 => pair2.Value.ToDictionary(
                        pair3 => pair3.Key,
                        pair3 => pair3.Value.Select(CreateShopItem).ToList())));
        }

        private ShopItem CreateShopItem(OrderDigest orderDigest)
        {
            var item = new ShopItem(orderDigest);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is ShopItem shopItemViewModel))
                {
                    return;
                }

                OnClickItem(shopItemViewModel.View);
            });

            return item;
        }

        protected void SelectItemView(ShopItemView view)
        {
            if (view == null || view.Model is null)
                return;

            DeselectItemView();
            _selectedItemViewModel.Value = view.Model;
            var item = Util.GetItemBaseByTradableId(view.Model.TradableId.Value, view.Model.ExpiredBlockIndex.Value);
            _selectedItemViewModel.Value.Selected.Value = true;
            _selectedItemViewModel.Value.ItemBase.Value = item;
            SelectedItemView.SetValueAndForceNotify(view);
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null ||
                _selectedItemViewModel.Value is null)
            {
                return;
            }

            _selectedItemViewModel.Value.Selected.Value = false;
            _selectedItemViewModel.Value = null;
            SelectedItemView.SetValueAndForceNotify(null);
        }

        public void ResetShopItems()
        {
            Items.Value = GetFilteredAndSortedProducts(_items);
            ResetSelectedState();
        }

        private Dictionary<int, List<ShopItem>> GetFilteredAndSortedProducts(IReadOnlyDictionary<
            ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>> products)
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
    }
}
