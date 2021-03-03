using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using ShopItem = Nekoyume.Model.Item.ShopItem;

namespace Nekoyume.State
{
    /// <summary>
    /// ShopState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveShopState
    {
        private enum SortType
        {
            None = 0,
            Grade = 1,
            Cp = 2,
        }

        public static readonly ReactiveProperty<Dictionary<
                Address, Dictionary<ItemSubTypeFilter, Dictionary<
                        SortFilter, Dictionary<int, List<ShopItem>>>>>>
            AgentProducts =
                new ReactiveProperty<Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter,
                        Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>>>();

        public static readonly ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>>>
            ItemSubTypeProducts = new ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>>>();

        private static int ShopItemsCountOfOnePage = 20;

        public static void Initialize(ShopState state, int shopItemsCountOfOnePage)
        {
            if (state is null)
            {
                return;
            }

            ShopItemsCountOfOnePage = shopItemsCountOfOnePage;

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
                        ItemSubTypeFilter, Dictionary<
                            SortFilter, Dictionary<int, List<ShopItem>>>>>();
                foreach (var pair in agentProducts)
                {
                    filteredAgentProducts.Add(
                        pair.Key,
                        GetGroupedShopItemsByItemSubTypeFilter(pair.Value));
                }

                AgentProducts.Value = filteredAgentProducts;
            }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeProducts.Value = GetGroupedShopItemsByItemSubTypeFilter(products
                    .Where(product => !product.SellerAgentAddress.Equals(agentAddress))
                    .ToList());
            }
        }

        private static Dictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(IReadOnlyCollection<ShopItem> shopItems)
        {
            var weapons = new List<ShopItem>();
            var armors = new List<ShopItem>();
            var belts = new List<ShopItem>();
            var necklaces = new List<ShopItem>();
            var rings = new List<ShopItem>();
            var foods = new List<ShopItem>();
            var fullCostumes = new List<ShopItem>();
            var hairCostumes = new List<ShopItem>();
            var earCostumes = new List<ShopItem>();
            var eyeCostumes = new List<ShopItem>();
            var tailCostumes = new List<ShopItem>();
            var titles = new List<ShopItem>();

            foreach (var shopItem in shopItems)
            {
                if (shopItem.ItemUsable != null)
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
                else if (shopItem.Costume != null)
                {
                    switch (shopItem.Costume.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            fullCostumes.Add(shopItem);
                            break;
                        case ItemSubType.HairCostume:
                            hairCostumes.Add(shopItem);
                            break;
                        case ItemSubType.EarCostume:
                            earCostumes.Add(shopItem);
                            break;
                        case ItemSubType.EyeCostume:
                            eyeCostumes.Add(shopItem);
                            break;
                        case ItemSubType.TailCostume:
                            tailCostumes.Add(shopItem);
                            break;
                        case ItemSubType.Title:
                            titles.Add(shopItem);
                            break;
                    }
                }
            }

            return GetGroupedShopItemsByItemSubTypeFilter(
                shopItems,
                weapons,
                armors,
                belts,
                necklaces,
                rings,
                foods,
                fullCostumes,
                hairCostumes,
                earCostumes,
                eyeCostumes,
                tailCostumes,
                titles);
        }

        private static Dictionary<
                ItemSubTypeFilter, Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(
                IReadOnlyCollection<ShopItem> all,
                IReadOnlyCollection<ShopItem> weapons,
                IReadOnlyCollection<ShopItem> armors,
                IReadOnlyCollection<ShopItem> belts,
                IReadOnlyCollection<ShopItem> necklaces,
                IReadOnlyCollection<ShopItem> rings,
                IReadOnlyCollection<ShopItem> foods,
                IReadOnlyCollection<ShopItem> fullCostumes,
                IReadOnlyCollection<ShopItem> hairCostumes,
                IReadOnlyCollection<ShopItem> earCostumes,
                IReadOnlyCollection<ShopItem> eyeCostumes,
                IReadOnlyCollection<ShopItem> tailCostumes,
                IReadOnlyCollection<ShopItem> titles)
        {
            return new Dictionary<
                ItemSubTypeFilter, Dictionary<
                    SortFilter, Dictionary<int, List<ShopItem>>>>
            {
                {ItemSubTypeFilter.All, GetGroupedShopItemsBySortFilter(all)},
                {ItemSubTypeFilter.Weapon, GetGroupedShopItemsBySortFilter(weapons)},
                {ItemSubTypeFilter.Armor, GetGroupedShopItemsBySortFilter(armors)},
                {ItemSubTypeFilter.Belt, GetGroupedShopItemsBySortFilter(belts)},
                {ItemSubTypeFilter.Necklace, GetGroupedShopItemsBySortFilter(necklaces)},
                {ItemSubTypeFilter.Ring, GetGroupedShopItemsBySortFilter(rings)},
                {ItemSubTypeFilter.Food, GetGroupedShopItemsBySortFilter(foods)},
                {ItemSubTypeFilter.FullCostume, GetGroupedShopItemsBySortFilter(fullCostumes)},
                {ItemSubTypeFilter.HairCostume, GetGroupedShopItemsBySortFilter(hairCostumes)},
                {ItemSubTypeFilter.EarCostume, GetGroupedShopItemsBySortFilter(earCostumes)},
                {ItemSubTypeFilter.EyeCostume, GetGroupedShopItemsBySortFilter(eyeCostumes)},
                {ItemSubTypeFilter.TailCostume, GetGroupedShopItemsBySortFilter(tailCostumes)},
                {ItemSubTypeFilter.Title, GetGroupedShopItemsBySortFilter(titles)},
            };
        }

        private static Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>
            GetGroupedShopItemsBySortFilter(IReadOnlyCollection<ShopItem> shopItems)
        {

            return new Dictionary<SortFilter, Dictionary<int, List<ShopItem>>>
            {
                {
                    SortFilter.Class,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Grade))
                },
                {
                    SortFilter.CP,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Cp))
                },
                {
                    SortFilter.Price,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => shopItem.Price)
                        .ToList())
                },
            };
        }

        private static List<ShopItem> GetSortedShopItems(IEnumerable<ShopItem> shopItems, SortType type)
        {
            var result = new List<ShopItem>();
            result.AddRange(shopItems.Where(shopItem => shopItem.Costume != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.Costume, type)));
            result.AddRange(shopItems.Where(shopItem => shopItem.ItemUsable != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.ItemUsable, type)));
            return result;
        }

        private static int GetTypeValue(ItemBase item, SortType type)
        {
            switch (type)
            {
                case SortType.Grade:
                    return item.Grade;
                case SortType.Cp:
                    switch (item)
                    {
                        case ItemUsable itemUsable:
                            return CPHelper.GetCP(itemUsable);
                        case Costume costume:
                        {
                            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                            return CPHelper.GetCP(costume, costumeSheet);
                        }
                    }
                    break;
                case SortType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
                var getCount = Math.Min(ShopItemsCountOfOnePage, remainCount);
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
