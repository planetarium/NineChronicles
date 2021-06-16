using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
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

        // public static readonly ReactiveProperty<Dictionary<
        //         Address, Dictionary<ItemSubTypeFilter, Dictionary<
        //             ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>
        //     AgentProducts =
        //         new ReactiveProperty<Dictionary<
        //             Address, Dictionary<
        //                 ItemSubTypeFilter,
        //                 Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>>();

        // public static readonly ReactiveProperty<IReadOnlyDictionary<
        //         ItemSubTypeFilter, Dictionary<
        //             ShopSortFilter, Dictionary<int, List<ShopItem>>>>>
        //     ItemSubTypeProducts = new ReactiveProperty<IReadOnlyDictionary<
        //         ItemSubTypeFilter, Dictionary<
        //             ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();

        public static readonly ReactiveProperty<Dictionary<
                Address, Dictionary<ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>>
            AgentDigests =
                new ReactiveProperty<Dictionary<
                    Address, Dictionary<
                        ItemSubTypeFilter,
                        Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>>();

        public static readonly ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>
            ItemSubTypeDigests = new ReactiveProperty<IReadOnlyDictionary<
                ItemSubTypeFilter, Dictionary<
                    ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>();

        public static readonly Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>> PurchaseHistory =
            new Dictionary<Guid, List<Nekoyume.UI.Model.ShopItem>>();

        private static List<ShopItem> _products = new List<ShopItem>();
        private static List<OrderDigest> _digests = new List<OrderDigest>();


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
            _products = state.Products.Values.ToList();
            _products.AddRange(shardedProducts);

            Update();
        }

        public static void Initialize(List<OrderDigest> digests)
        {
            _digests = digests;
            Update();
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
            {
                // var agentProducts = new Dictionary<Address, List<ShopItem>>();
                // foreach (var product in _products)
                // {
                //     var agentAddress = product.SellerAgentAddress;
                //     if (!agentProducts.ContainsKey(agentAddress))
                //     {
                //         agentProducts.Add(agentAddress, new List<ShopItem>());
                //     }
                //
                //     if (Game.Game.instance.Agent.Address == agentAddress)
                //     {
                //         if (product.SellerAvatarAddress == States.Instance.CurrentAvatarState.address)
                //         {
                //             agentProducts[agentAddress].Add(product);
                //         }
                //     }
                //     else
                //     {
                //         agentProducts[agentAddress].Add(product);
                //     }
                // }
                var agentDigests = new Dictionary<Address, List<OrderDigest>>();
                foreach (var product in _digests)
                {
                    var agentAddress = product.SellerAgentAddress;
                    if (!agentDigests.ContainsKey(agentAddress))
                    {
                        agentDigests.Add(agentAddress, new List<OrderDigest>());
                    }

                    agentDigests[agentAddress].Add(product);
                }

                // var filteredAgentProducts = new Dictionary<
                //     Address,
                //     Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<ShopItem>>>>>();
                // foreach (var pair in agentProducts)
                // {
                //     filteredAgentProducts.Add(pair.Key, GetGroupedShopItemsByItemSubTypeFilter(pair.Value, sellItemsPerPage));
                // }
                //
                // AgentProducts.Value = filteredAgentProducts;

                var filteredAgentDigests = new Dictionary<
                    Address,
                    Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>();
                foreach (var pair in agentDigests)
                {
                    filteredAgentDigests.Add(pair.Key, GetGroupedOrderDigestsByItemSubTypeFilter(pair.Value, sellItemsPerPage));
                }

                AgentDigests.Value = filteredAgentDigests;

            }

            // ItemSubTypeProducts.
            {
                var agentAddress = States.Instance.AgentState.address;
                ItemSubTypeDigests.Value = GetGroupedOrderDigestsByItemSubTypeFilter(_digests
                    .Where(product => !product.SellerAgentAddress.Equals(agentAddress)).ToList(), buyItemsPerPage);
            }
        }

        private static Dictionary<ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>
            GetGroupedOrderDigestsByItemSubTypeFilter(List<OrderDigest> digests, int shopItemsPerPage)
        {
            var equipment = new List<OrderDigest>();
            var food = new List<OrderDigest>();
            var costume = new List<OrderDigest>();
            var weapons = new List<OrderDigest>();
            var armors = new List<OrderDigest>();
            var belts = new List<OrderDigest>();
            var necklaces = new List<OrderDigest>();
            var rings = new List<OrderDigest>();
            var foodsHp = new List<OrderDigest>();
            var foodsAtk = new List<OrderDigest>();
            var foodsDef = new List<OrderDigest>();
            var foodsCri = new List<OrderDigest>();
            var foodsHit = new List<OrderDigest>();
            var fullCostumes = new List<OrderDigest>();
            var hairCostumes = new List<OrderDigest>();
            var earCostumes = new List<OrderDigest>();
            var eyeCostumes = new List<OrderDigest>();
            var tailCostumes = new List<OrderDigest>();
            var titles = new List<OrderDigest>();
            var materials = new List<OrderDigest>();

            foreach (var digest in digests)
            {
                var itemId = digest.ItemId;
                var row = Game.Game.instance.TableSheets.ItemSheet[itemId];
                var itemSubType = row.ItemSubType;
                if (itemSubType == ItemSubType.Food)
                {
                    food.Add(digest);
                    var consumableRow = (ConsumableItemSheet.Row) row;
                    var state = consumableRow.Stats.First();
                    switch (state.StatType)
                    {
                        case StatType.HP:
                            foodsHp.Add(digest);
                            break;
                        case StatType.ATK:
                            foodsAtk.Add(digest);
                            break;
                        case StatType.DEF:
                            foodsDef.Add(digest);
                            break;
                        case StatType.CRI:
                            foodsCri.Add(digest);
                            break;
                        case StatType.HIT:
                            foodsHit.Add(digest);
                            break;
                    }
                }
                else
                {
                    if (row.ItemType == ItemType.Equipment)
                    {
                        equipment.Add(digest);
                    }

                    if (row.ItemType == ItemType.Costume)
                    {
                        costume.Add(digest);
                    }
                    switch (row.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            weapons.Add(digest);
                            break;
                        case ItemSubType.Armor:
                            armors.Add(digest);
                            break;
                        case ItemSubType.Belt:
                            belts.Add(digest);
                            break;
                        case ItemSubType.Necklace:
                            necklaces.Add(digest);
                            break;
                        case ItemSubType.Ring:
                            rings.Add(digest);
                            break;
                        case ItemSubType.FullCostume:
                            fullCostumes.Add(digest);
                            break;
                        case ItemSubType.HairCostume:
                            hairCostumes.Add(digest);
                            break;
                        case ItemSubType.EarCostume:
                            earCostumes.Add(digest);
                            break;
                        case ItemSubType.EyeCostume:
                            eyeCostumes.Add(digest);
                            break;
                        case ItemSubType.TailCostume:
                            tailCostumes.Add(digest);
                            break;
                        case ItemSubType.Title:
                            titles.Add(digest);
                            break;
                        case ItemSubType.Hourglass:
                        case ItemSubType.ApStone:
                            materials.Add(digest);
                            break;
                    }
                }
            }
            var groupedOrderDigests = new Dictionary<
                ItemSubTypeFilter, Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>
            {
                {ItemSubTypeFilter.All, GetGroupedOrderDigestsBySortFilter(digests, shopItemsPerPage)},
                {ItemSubTypeFilter.Weapon, GetGroupedOrderDigestsBySortFilter(weapons, shopItemsPerPage)},
                {ItemSubTypeFilter.Armor, GetGroupedOrderDigestsBySortFilter(armors, shopItemsPerPage)},
                {ItemSubTypeFilter.Belt, GetGroupedOrderDigestsBySortFilter(belts, shopItemsPerPage)},
                {ItemSubTypeFilter.Necklace, GetGroupedOrderDigestsBySortFilter(necklaces, shopItemsPerPage)},
                {ItemSubTypeFilter.Ring, GetGroupedOrderDigestsBySortFilter(rings, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_HP, GetGroupedOrderDigestsBySortFilter(foodsHp, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_ATK, GetGroupedOrderDigestsBySortFilter(foodsAtk, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_DEF, GetGroupedOrderDigestsBySortFilter(foodsDef, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_CRI, GetGroupedOrderDigestsBySortFilter(foodsCri, shopItemsPerPage)},
                {ItemSubTypeFilter.Food_HIT, GetGroupedOrderDigestsBySortFilter(foodsHit, shopItemsPerPage)},
                {ItemSubTypeFilter.FullCostume, GetGroupedOrderDigestsBySortFilter(fullCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.HairCostume, GetGroupedOrderDigestsBySortFilter(hairCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.EarCostume, GetGroupedOrderDigestsBySortFilter(earCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.EyeCostume, GetGroupedOrderDigestsBySortFilter(eyeCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.TailCostume, GetGroupedOrderDigestsBySortFilter(tailCostumes, shopItemsPerPage)},
                {ItemSubTypeFilter.Title, GetGroupedOrderDigestsBySortFilter(titles, shopItemsPerPage)},
                {ItemSubTypeFilter.Materials, GetGroupedOrderDigestsBySortFilter(materials, shopItemsPerPage)},
            };
            return groupedOrderDigests;
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

        private static Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>
            GetGroupedOrderDigestsBySortFilter(IReadOnlyCollection<OrderDigest> digests, int shopItemsPerPage)
        {
            return new Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>
            {
                {
                    ShopSortFilter.Class,
                    GetGroupedShopItemsByPage(GetSortedOrderDigests(digests, SortType.Grade), shopItemsPerPage)
                },
                {
                    ShopSortFilter.CP,
                    GetGroupedShopItemsByPage(GetSortedOrderDigests(digests, SortType.Cp), shopItemsPerPage)
                },
                {
                    ShopSortFilter.Price,
                    GetGroupedShopItemsByPage(digests
                        .OrderByDescending(digest => digest.Price).ToList(), shopItemsPerPage)
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
            result.AddRange(shopItems.Where(shopItem => shopItem.TradableFungibleItem != null)
                .OrderByDescending(shopItem => GetTypeValue((ItemBase)shopItem.TradableFungibleItem, type)));
            return result;
        }

        private static List<OrderDigest> GetSortedOrderDigests(IEnumerable<OrderDigest> digests, SortType type)
        {
            var result = new List<OrderDigest>();
            var orderDigests = digests.ToList();
            var costumes = orderDigests.Where(d => Game.Game.instance.TableSheets.CostumeItemSheet.ContainsKey(d.ItemId)).ToList();
            var materials = orderDigests.Where(d => Game.Game.instance.TableSheets.MaterialItemSheet.ContainsKey(d.ItemId)).ToList();
            var itemUsables = orderDigests.Where(d => !costumes.Contains(d) && !materials.Contains(d));
            result.AddRange(costumes.OrderByDescending(digest => GetTypeValue(digest, type)));
            result.AddRange(itemUsables.OrderByDescending(digest => GetTypeValue(digest, type)));
            result.AddRange(materials.OrderByDescending(digest => GetTypeValue(digest, type)));
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

        private static int GetTypeValue(OrderDigest item, SortType type)
        {
            switch (type)
            {
                case SortType.Grade:
                    return Game.Game.instance.TableSheets.ItemSheet[item.ItemId].Grade;
                case SortType.Cp:
                    return item.CombatPoint;
                case SortType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        private static Dictionary<int, List<ShopItem>> GetGroupedShopItemsByPage(List<ShopItem> shopItems, int shopItemsPerPage)
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

        private static Dictionary<int, List<OrderDigest>> GetGroupedShopItemsByPage(List<OrderDigest> digests, int shopItemsPerPage)
        {
            var result = new Dictionary<int, List<OrderDigest>>();
            var remainCount = digests.Count;
            var listIndex = 0;
            var pageIndex = 0;
            while (remainCount > 0)
            {
                var getCount = Math.Min(shopItemsPerPage, remainCount);
                var getList = digests.GetRange(listIndex, getCount);
                result.Add(pageIndex, getList);
                remainCount -= getCount;
                listIndex += getCount;
                pageIndex++;
            }

            return result;
        }
    }
}
