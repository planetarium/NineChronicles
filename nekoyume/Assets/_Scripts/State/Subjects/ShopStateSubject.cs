using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.State.Subjects
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ShopStateSubject
    {
        public static readonly Subject<Dictionary<
                Address, Dictionary<
                    ShopItems.ItemSubTypeFilter, Dictionary<
                        ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>>
            AgentProducts =
                new Subject<Dictionary<
                    Address, Dictionary<
                        ShopItems.ItemSubTypeFilter,
                        Dictionary<ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>>();

        public static readonly Subject<IReadOnlyDictionary<
                ShopItems.ItemSubTypeFilter, Dictionary<
                    ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>
            ItemSubTypeProducts = new Subject<IReadOnlyDictionary<
                ShopItems.ItemSubTypeFilter, Dictionary<
                    ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>();

        public static void Initialize(ShopState state)
        {
            if (state is null)
            {
                return;
            }

            var products = state.Products.Values.ToList();

            // AgentProducts.
            {
                var agentProducts = new Dictionary<Address, List<ShopItem>>();
                foreach (var product in products)
                {
                    var agentAddress = product.SellerAgentAddress;
                    if (!agentProducts.ContainsKey(agentAddress))
                    {
                        agentProducts.Add(agentAddress, new List<ShopItem>());
                    }

                    agentProducts[agentAddress].Add(product);
                }

                var filteredAgentProducts = new Dictionary<
                    Address, Dictionary<
                        ShopItems.ItemSubTypeFilter, Dictionary<
                            ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>>();
                foreach (var pair in agentProducts)
                {
                    filteredAgentProducts.Add(
                        pair.Key,
                        GetGroupedShopItemsByItemSubTypeFilter(pair.Value));
                }

                AgentProducts.OnNext(filteredAgentProducts);
            }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeProducts.OnNext(GetGroupedShopItemsByItemSubTypeFilter(products
                    .Where(product => !product.SellerAgentAddress.Equals(agentAddress))
                    .ToList()));
            }
        }

        private static Dictionary<
                ShopItems.ItemSubTypeFilter, Dictionary<
                    ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(List<ShopItem> shopItems)
        {
            var weapons = new List<ShopItem>();
            var armors = new List<ShopItem>();
            var belts = new List<ShopItem>();
            var necklaces = new List<ShopItem>();
            var rings = new List<ShopItem>();
            var foods = new List<ShopItem>();

            foreach (var shopItem in shopItems)
            {
                switch (shopItem.ItemUsable.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        weapons.Add(shopItem);
                        break;
                    case ItemSubType.Armor:
                        armors.Add(shopItem);
                        break;
                    case ItemSubType.Belt:
                        belts.Add(shopItem);
                        break;
                    case ItemSubType.Necklace:
                        necklaces.Add(shopItem);
                        break;
                    case ItemSubType.Ring:
                        rings.Add(shopItem);
                        break;
                    case ItemSubType.Food:
                        foods.Add(shopItem);
                        break;
                }
            }

            return GetGroupedShopItemsByItemSubTypeFilter(
                shopItems,
                weapons,
                armors,
                belts,
                necklaces,
                rings,
                foods);
        }

        private static Dictionary<
                ShopItems.ItemSubTypeFilter, Dictionary<
                    ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(
                IReadOnlyCollection<ShopItem> all,
                IReadOnlyCollection<ShopItem> weapons,
                IReadOnlyCollection<ShopItem> armors,
                IReadOnlyCollection<ShopItem> belts,
                IReadOnlyCollection<ShopItem> necklaces,
                IReadOnlyCollection<ShopItem> rings,
                IReadOnlyCollection<ShopItem> foods)
        {
            return new Dictionary<
                ShopItems.ItemSubTypeFilter, Dictionary<
                    ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>>
            {
                {ShopItems.ItemSubTypeFilter.All, GetGroupedShopItemsBySortFilter(all)},
                {ShopItems.ItemSubTypeFilter.Weapon, GetGroupedShopItemsBySortFilter(weapons)},
                {ShopItems.ItemSubTypeFilter.Armor, GetGroupedShopItemsBySortFilter(armors)},
                {ShopItems.ItemSubTypeFilter.Belt, GetGroupedShopItemsBySortFilter(belts)},
                {ShopItems.ItemSubTypeFilter.Necklace, GetGroupedShopItemsBySortFilter(necklaces)},
                {ShopItems.ItemSubTypeFilter.Ring, GetGroupedShopItemsBySortFilter(rings)},
                {ShopItems.ItemSubTypeFilter.Food, GetGroupedShopItemsBySortFilter(foods)},
            };
        }

        private static Dictionary<ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>
            GetGroupedShopItemsBySortFilter(IReadOnlyCollection<ShopItem> shopItems)
        {
            return new Dictionary<ShopItems.SortFilter, Dictionary<int, List<ShopItem>>>
            {
                {
                    ShopItems.SortFilter.Class,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => shopItem.ItemUsable.Grade)
                        .ToList())
                },
                {
                    ShopItems.SortFilter.CP,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => CPHelper.GetCP(shopItem.ItemUsable))
                        .ToList())
                },
                {
                    ShopItems.SortFilter.Price,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => shopItem.Price)
                        .ToList())
                },
            };
        }

        private static Dictionary<int, List<ShopItem>> GetGroupedShopItemsByPage(
            List<ShopItem> shopItems)
        {
            var result = new Dictionary<int, List<ShopItem>>();
            var remainCount = shopItems.Count;
            var listIndex = 0;
            var pageIndex = 0;
            while (remainCount > 0)
            {
                var getCount = Math.Min(ShopItems.shopItemsCountOfOnePage, remainCount);
                var getList = shopItems.GetRange(listIndex, getCount);
                result.Add(pageIndex, getList);
                remainCount -= getCount;
                listIndex += getCount;
                pageIndex++;
            }

            return result;
        }
    }
}
