using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using ShopItem = Nekoyume.Model.Item.ShopItem;
using BxDictionary = Bencodex.Types.Dictionary;

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
                        ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>
            AgentProducts =
                new ReactiveProperty<Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter,
                        Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>();
        
        public static readonly ReactiveProperty<Dictionary<
                Address, Dictionary<ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>>>
            AgentProductsV2 =
                new ReactiveProperty<Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter,
                        Dictionary<ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>>>();

        public static readonly ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>>
            ItemSubTypeProducts = new ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();
        
        public static readonly ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>>
            ItemSubTypeProductsV2 = new ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>>();

        public static readonly Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>> PurchaseHistory =
            new Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>>();

        private static List<ShopItem> _products = new List<ShopItem>();
        private static List<(Order, ItemBase)> _productsV2 = new List<(Order, ItemBase)>();


        private const int buyItemsPerPage = 24;
        private const int sellItemsPerPage = 20;


        public static void Initialize(ShopState state, IEnumerable<ShopItem> shardedProducts)
        {
            if (state is null)
            {
                return;
            }

            // It uses shaded shop state with the old store state.
            // Later, only shaded shop state will be used.
            // _products = state.Products.Values.ToList();
            _products.AddRange(shardedProducts);

            Update();
        }
        
        public static void InitializeV2(ShopState state, IEnumerable<Order> shardedProductsV2)
        {
            if (state is null)
            {
                return;
            }

            var agent = Game.Game.instance.Agent;
            var tuples = shardedProductsV2.Select(order =>
            {
                var itemAddress = Addresses.GetItemAddress(order.TradableId);
                var itemValue = agent.GetState(itemAddress);
                if (!(itemValue is BxDictionary itemDict))
                {
                    return (order, null);
                }

                return (order, ItemFactory.Deserialize(itemDict));
            });
            
            _productsV2.AddRange(tuples);

            UpdateV2();
        }

        public static void RemoveShopItem(Guid productId)
        {

            var item = _products.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                _products.Remove(item);
            }

            Update();
        }

        public static void Update()
        {
            // AgentProducts.
            // {
            //     var agentProducts = new Dictionary<Address, List<ShopItem>>();
            //     foreach (var product in _products)
            //     {
            //         var agentAddress = product.SellerAgentAddress;
            //         if (!agentProducts.ContainsKey(agentAddress))
            //         {
            //             agentProducts.Add(agentAddress, new List<ShopItem>());
            //         }
            //
            //         if (Game.Game.instance.Agent.Address == agentAddress)
            //         {
            //             if (product.SellerAvatarAddress == States.Instance.CurrentAvatarState.address)
            //             {
            //                 agentProducts[agentAddress].Add(product);
            //             }
            //         }
            //         else
            //         {
            //             agentProducts[agentAddress].Add(product);
            //         }
            //     }
            //
            //     var filteredAgentProducts = new Dictionary<
            //         Address,
            //         Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();
            //     foreach (var pair in agentProducts)
            //     {
            //         filteredAgentProducts.Add(pair.Key, GetGroupedShopItemsByItemSubTypeFilter(pair.Value, sellItemsPerPage));
            //     }
            //
            //     AgentProducts.Value = filteredAgentProducts;
            // }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeProducts.Value = GetGroupedShopItemsByItemSubTypeFilter(
                    _products
                    .Where(product => !product.SellerAgentAddress.Equals(agentAddress))
                    .ToList(),
                    buyItemsPerPage);
            }
        }
        
        public static void UpdateV2()
        {
            // AgentProducts.
            // {
            //     var agentProducts = new Dictionary<Address, List<(Order, ItemBase)>>();
            //     foreach (var tuple in _productsV2)
            //     {
            //         var (order, tradableItem) = tuple;
            //         var agentAddress = order.SellerAgentAddress;
            //         if (!agentProducts.ContainsKey(agentAddress))
            //         {
            //             agentProducts.Add(agentAddress, new List<(Order, ItemBase)>());
            //         }
            //
            //         if (Game.Game.instance.Agent.Address == agentAddress)
            //         {
            //             if (order.SellerAvatarAddress == States.Instance.CurrentAvatarState.address)
            //             {
            //                 agentProducts[agentAddress].Add((order, tradableItem));
            //             }
            //         }
            //         else
            //         {
            //             agentProducts[agentAddress].Add((order, tradableItem));
            //         }
            //     }
            //
            //     AgentProductsV2.Value = agentProducts
            //         .ToDictionary(
            //             pair => pair.Key,
            //             pair => GetGroupedShopItemsByItemSubTypeFilterV2(pair.Value, sellItemsPerPage));
            // }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeProductsV2.Value = GetGroupedShopItemsByItemSubTypeFilterV2(
                    _productsV2
                        .Where(tuple => !tuple.Item1.SellerAgentAddress.Equals(agentAddress))
                        .ToList(),
                    buyItemsPerPage);
            }
        }

        private static Dictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<ShopItem>>>>
            GetGroupedShopItemsByItemSubTypeFilter(IReadOnlyCollection<ShopItem> shopItems, int shopItemsPerPage)
        {
            var equipment = new List<ShopItem>();
            var food = new List<ShopItem>();
            var costume = new List<ShopItem>();
            var weapons = new List<ShopItem>();
            var armors = new List<ShopItem>();
            var belts = new List<ShopItem>();
            var necklaces = new List<ShopItem>();
            var rings = new List<ShopItem>();
            var foodsHp = new List<ShopItem>();
            var foodsAtk = new List<ShopItem>();
            var foodsDef = new List<ShopItem>();
            var foodsCri = new List<ShopItem>();
            var foodsHit = new List<ShopItem>();
            var fullCostumes = new List<ShopItem>();
            var hairCostumes = new List<ShopItem>();
            var earCostumes = new List<ShopItem>();
            var eyeCostumes = new List<ShopItem>();
            var tailCostumes = new List<ShopItem>();
            var titles = new List<ShopItem>();
            var materials = new List<ShopItem>();

            foreach (var shopItem in shopItems)
            {
                if (shopItem.ItemUsable != null)
                {
                    if (shopItem.ItemUsable.ItemSubType == ItemSubType.Food)
                    {
                        food.Add(shopItem);
                        var state = shopItem.ItemUsable.StatsMap.GetStats().First();
                        switch (state.StatType)
                        {
                            case StatType.HP:
                                foodsHp.Add(shopItem);
                                break;
                            case StatType.ATK:
                                foodsAtk.Add(shopItem);
                                break;
                            case StatType.DEF:
                                foodsDef.Add(shopItem);
                                break;
                            case StatType.CRI:
                                foodsCri.Add(shopItem);
                                break;
                            case StatType.HIT:
                                foodsHit.Add(shopItem);
                                break;
                        }
                    }
                    else
                    {
                        equipment.Add(shopItem);
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
                        }
                    }
                }
                else if (shopItem.Costume != null)
                {
                    costume.Add(shopItem);
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
                else
                {
                    // Currently, there are only hourglass and AP potions.
                    materials.Add(shopItem);
                }
            }

            var groupedShopItems = new Dictionary<
                ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>
            {
                {ItemSubTypeFilter.All, GetGroupedShopItemsBySortFilter(shopItems, shopItemsPerPage)},
                {ItemSubTypeFilter.Weapon, GetGroupedShopItemsBySortFilter(weapons, shopItemsPerPage)},
                {ItemSubTypeFilter.Armor, GetGroupedShopItemsBySortFilter(armors, shopItemsPerPage)},
                {ItemSubTypeFilter.Belt, GetGroupedShopItemsBySortFilter(belts, shopItemsPerPage)},
                {ItemSubTypeFilter.Necklace, GetGroupedShopItemsBySortFilter(necklaces, shopItemsPerPage)},
                {ItemSubTypeFilter.Ring, GetGroupedShopItemsBySortFilter(rings, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_HP, GetGroupedShopItemsBySortFilter(foodsHp, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_ATK, GetGroupedShopItemsBySortFilter(foodsAtk, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_DEF, GetGroupedShopItemsBySortFilter(foodsDef, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_CRI, GetGroupedShopItemsBySortFilter(foodsCri, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_HIT, GetGroupedShopItemsBySortFilter(foodsHit, shopItemsPerPage)},
                {ItemSubTypeFilter.FullCostume, GetGroupedShopItemsBySortFilter(fullCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.HairCostume, GetGroupedShopItemsBySortFilter(hairCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.EarCostume, GetGroupedShopItemsBySortFilter(earCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.EyeCostume, GetGroupedShopItemsBySortFilter(eyeCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.TailCostume, GetGroupedShopItemsBySortFilter(tailCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.Title, GetGroupedShopItemsBySortFilter(titles, shopItemsPerPage)},
                {ItemSubTypeFilter.Materials, GetGroupedShopItemsBySortFilter(materials, shopItemsPerPage)},
            };
            return groupedShopItems;
        }
        
        private static Dictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>
            GetGroupedShopItemsByItemSubTypeFilterV2(IReadOnlyCollection<(Order, ItemBase)> tuples, int ordersPerPage)
        {
            var equipments = new List<(Order, ItemBase)>();
            var foods = new List<(Order, ItemBase)>();
            var costumes = new List<(Order, ItemBase)>();
            var weapons = new List<(Order, ItemBase)>();
            var armors = new List<(Order, ItemBase)>();
            var belts = new List<(Order, ItemBase)>();
            var necklaces = new List<(Order, ItemBase)>();
            var rings = new List<(Order, ItemBase)>();
            var foodsHp = new List<(Order, ItemBase)>();
            var foodsAtk = new List<(Order, ItemBase)>();
            var foodsDef = new List<(Order, ItemBase)>();
            var foodsCri = new List<(Order, ItemBase)>();
            var foodsHit = new List<(Order, ItemBase)>();
            var fullCostumes = new List<(Order, ItemBase)>();
            var hairCostumes = new List<(Order, ItemBase)>();
            var earCostumes = new List<(Order, ItemBase)>();
            var eyeCostumes = new List<(Order, ItemBase)>();
            var tailCostumes = new List<(Order, ItemBase)>();
            var titles = new List<(Order, ItemBase)>();
            var materials = new List<(Order, ItemBase)>();

            var agent = Game.Game.instance.Agent;
            foreach (var tuple in tuples)
            {
                var (order, tradableItem) = tuple;
                var itemAddress = Addresses.GetItemAddress(order.TradableId);
                var itemValue = agent.GetState(itemAddress);
                if (itemValue is null ||
                    !(itemValue is BxDictionary itemDict))
                {
                    Debug.LogWarning($"item is null. {itemAddress}");
                    continue;
                }

                var itemBase = ItemFactory.Deserialize(itemDict);
                if (itemBase is ItemUsable itemUsable)
                {
                    if (itemUsable.ItemSubType == ItemSubType.Food)
                    {
                        foods.Add(tuple);
                        var state = itemUsable.StatsMap.GetStats().First();
                        switch (state.StatType)
                        {
                            case StatType.HP:
                                foodsHp.Add(tuple);
                                break;
                            case StatType.ATK:
                                foodsAtk.Add(tuple);
                                break;
                            case StatType.DEF:
                                foodsDef.Add(tuple);
                                break;
                            case StatType.CRI:
                                foodsCri.Add(tuple);
                                break;
                            case StatType.HIT:
                                foodsHit.Add(tuple);
                                break;
                        }
                    }
                    else
                    {
                        equipments.Add(tuple);
                        switch (itemUsable.ItemSubType)
                        {
                            case ItemSubType.Weapon:
                                weapons.Add(tuple);
                                break;
                            case ItemSubType.Armor:
                                armors.Add(tuple);
                                break;
                            case ItemSubType.Belt:
                                belts.Add(tuple);
                                break;
                            case ItemSubType.Necklace:
                                necklaces.Add(tuple);
                                break;
                            case ItemSubType.Ring:
                                rings.Add(tuple);
                                break;
                        }
                    }
                }
                else if (itemBase is Costume costume)
                {
                    costumes.Add(tuple);
                    switch (costume.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            fullCostumes.Add(tuple);
                            break;
                        case ItemSubType.HairCostume:
                            hairCostumes.Add(tuple);
                            break;
                        case ItemSubType.EarCostume:
                            earCostumes.Add(tuple);
                            break;
                        case ItemSubType.EyeCostume:
                            eyeCostumes.Add(tuple);
                            break;
                        case ItemSubType.TailCostume:
                            tailCostumes.Add(tuple);
                            break;
                        case ItemSubType.Title:
                            titles.Add(tuple);
                            break;
                    }
                }
                else
                {
                    // Currently, there are only hourglass and AP potions.
                    materials.Add(tuple);
                }
            }

            var groupedShopItems = new Dictionary<
                ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>>
            {
                {ItemSubTypeFilter.All, GetGroupedShopItemsBySortFilterV2(tuples, ordersPerPage)},
                {ItemSubTypeFilter.Weapon, GetGroupedShopItemsBySortFilterV2(weapons, ordersPerPage)},
                {ItemSubTypeFilter.Armor, GetGroupedShopItemsBySortFilterV2(armors, ordersPerPage)},
                {ItemSubTypeFilter.Belt, GetGroupedShopItemsBySortFilterV2(belts, ordersPerPage)},
                {ItemSubTypeFilter.Necklace, GetGroupedShopItemsBySortFilterV2(necklaces, ordersPerPage)},
                {ItemSubTypeFilter.Ring, GetGroupedShopItemsBySortFilterV2(rings, ordersPerPage)},
                {ItemSubTypeFilter.Food_HP, GetGroupedShopItemsBySortFilterV2(foodsHp, ordersPerPage)},
                {ItemSubTypeFilter.Food_ATK, GetGroupedShopItemsBySortFilterV2(foodsAtk, ordersPerPage)},
                {ItemSubTypeFilter.Food_DEF, GetGroupedShopItemsBySortFilterV2(foodsDef, ordersPerPage)},
                {ItemSubTypeFilter.Food_CRI, GetGroupedShopItemsBySortFilterV2(foodsCri, ordersPerPage)},
                {ItemSubTypeFilter.Food_HIT, GetGroupedShopItemsBySortFilterV2(foodsHit, ordersPerPage)},
                {ItemSubTypeFilter.FullCostume, GetGroupedShopItemsBySortFilterV2(fullCostumes, ordersPerPage)},
                {ItemSubTypeFilter.HairCostume, GetGroupedShopItemsBySortFilterV2(hairCostumes, ordersPerPage)},
                {ItemSubTypeFilter.EarCostume, GetGroupedShopItemsBySortFilterV2(earCostumes, ordersPerPage)},
                {ItemSubTypeFilter.EyeCostume, GetGroupedShopItemsBySortFilterV2(eyeCostumes, ordersPerPage)},
                {ItemSubTypeFilter.TailCostume, GetGroupedShopItemsBySortFilterV2(tailCostumes, ordersPerPage)},
                {ItemSubTypeFilter.Title, GetGroupedShopItemsBySortFilterV2(titles, ordersPerPage)},
                {ItemSubTypeFilter.Materials, GetGroupedShopItemsBySortFilterV2(materials, ordersPerPage)},
            };
            return groupedShopItems;
        }

        private static Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>
            GetGroupedShopItemsBySortFilter(IReadOnlyCollection<ShopItem> shopItems, int shopItemsPerPage)
        {
            return new Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>
            {
                {
                    ShopSortFilter.Class,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Grade), shopItemsPerPage)
                },
                {
                    ShopSortFilter.CP,
                    GetGroupedShopItemsByPage(GetSortedShopItems(shopItems, SortType.Cp), shopItemsPerPage)
                },
                {
                    ShopSortFilter.Price,
                    GetGroupedShopItemsByPage(shopItems
                        .OrderByDescending(shopItem => shopItem.Price).ToList(), shopItemsPerPage)
                },
            };
        }
        
        private static Dictionary<ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>
            GetGroupedShopItemsBySortFilterV2(IReadOnlyCollection<(Order, ItemBase)> tuples, int shopItemsPerPage)
        {
            return new Dictionary<ShopSortFilter, Dictionary<int, List<(Order, ItemBase)>>>
            {
                {
                    ShopSortFilter.Class,
                    GetGroupedShopItemsByPageV2(GetSortedShopItemsV2(tuples, SortType.Grade), shopItemsPerPage)
                },
                {
                    ShopSortFilter.CP,
                    GetGroupedShopItemsByPageV2(GetSortedShopItemsV2(tuples, SortType.Cp), shopItemsPerPage)
                },
                {
                    ShopSortFilter.Price,
                    GetGroupedShopItemsByPageV2(tuples
                        .OrderByDescending(tuple => tuple.Item1.Price).ToList(), shopItemsPerPage)
                },
            };
        }

        private static List<ShopItem> GetSortedShopItems(IReadOnlyCollection<ShopItem> shopItems, SortType type)
        {
            var result = new List<ShopItem>();
            result.AddRange(shopItems.Where(shopItem => shopItem.Costume != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.Costume, type)));
            result.AddRange(shopItems.Where(shopItem => shopItem.ItemUsable != null)
                .OrderByDescending(shopItem => GetTypeValue(shopItem.ItemUsable, type)));
            result.AddRange(shopItems.Where(shopItem => shopItem.TradableFungibleItem != null)
                .OrderByDescending(shopItem => GetTypeValue((ItemBase)shopItem.TradableFungibleItem, type)));
            return result;
        }
        
        private static List<(Order, ItemBase)> GetSortedShopItemsV2(
            IReadOnlyCollection<(Order, ItemBase)> tuples,
            SortType type)
        {
            var result = new List<(Order, ItemBase)>();
            result.AddRange(tuples.Where(tuple => tuple.Item2 is Costume)
                .OrderByDescending(tuple => GetTypeValue(tuple.Item2, type)));
            result.AddRange(tuples.Where(tuple => tuple.Item2 is ItemUsable)
                .OrderByDescending(tuple => GetTypeValue(tuple.Item2, type)));
            result.AddRange(tuples.Where(tuple => tuple.Item2 is ITradableFungibleItem)
                .OrderByDescending(tuple => GetTypeValue(tuple.Item2, type)));
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
                        default:
                            return 0;
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
            List<ShopItem> shopItems,
            int shopItemsPerPage)
        {
            var result = new Dictionary<int, List<ShopItem>>();
            var remainCount = shopItems.Count;
            var listIndex = 0;
            var pageIndex = 0;
            while (remainCount > 0)
            {
                var getCount = Math.Min(shopItemsPerPage, remainCount);
                var getList = shopItems.GetRange(listIndex, getCount);
                result.Add(pageIndex, getList);
                remainCount -= getCount;
                listIndex += getCount;
                pageIndex++;
            }

            return result;
        }
        
        private static Dictionary<int, List<(Order, ItemBase)>> GetGroupedShopItemsByPageV2(
            List<(Order, ItemBase)> tuples,
            int shopItemsPerPage)
        {
            var result = new Dictionary<int, List<(Order, ItemBase)>>();
            var remainCount = tuples.Count;
            var listIndex = 0;
            var pageIndex = 0;
            while (remainCount > 0)
            {
                var getCount = Math.Min(shopItemsPerPage, remainCount);
                var getList = tuples.GetRange(listIndex, getCount);
                result.Add(pageIndex, getList);
                remainCount -= getCount;
                listIndex += getCount;
                pageIndex++;
            }

            return result;
        }
    }
}
