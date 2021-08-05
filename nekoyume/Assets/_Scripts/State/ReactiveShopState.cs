using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.State
{
    /// <summary>
    /// Changes in the values included in ShopState are notified to the outside through each ReactiveProperty<T> field.
    /// </summary>
    public static class ReactiveShopState
    {
        private enum SortType
        {
            None = 0,
            Grade = 1,
            Cp = 2,
        }

        private static readonly List<ItemSubType> ItemSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.FullCostume,
            ItemSubType.HairCostume,
            ItemSubType.EarCostume,
            ItemSubType.EyeCostume,
            ItemSubType.TailCostume,
            ItemSubType.Title,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        private static readonly List<ItemSubType> ShardedSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        public static readonly
            ReactiveProperty<IReadOnlyDictionary<ItemSubTypeFilter,
                Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>> BuyDigests =
                new ReactiveProperty<IReadOnlyDictionary<ItemSubTypeFilter,
                    Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>();

        public static readonly
            ReactiveProperty<IReadOnlyDictionary<ItemSubTypeFilter,
                Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>> SellDigests =
                new ReactiveProperty<IReadOnlyDictionary<ItemSubTypeFilter,
                    Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>>();

        private static List<OrderDigest> _buyDigests = new List<OrderDigest>();
        private static List<OrderDigest> _sellDigests = new List<OrderDigest>();

        public static bool IsExistSellDigests(ItemBase itemBase, int count = 1)
        {
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    var consumable = (Consumable) itemBase;
                    return _sellDigests.Exists(x => x.TradableId.Equals(consumable.ItemId));

                case ItemType.Costume:
                    var costume = (Costume) itemBase;
                    return _sellDigests.Exists(x => x.TradableId.Equals(costume.ItemId));
                case ItemType.Equipment:
                    var equipment = (Equipment) itemBase;
                    return _sellDigests.Exists(x => x.TradableId.Equals(equipment.ItemId));
                case ItemType.Material:
                    var material = (Material) itemBase;
                    if (material is TradableMaterial tradableMaterial)
                    {
                        return _sellDigests.Exists(x =>
                            x.TradableId.Equals(tradableMaterial.TradableId) &&
                            x.ExpiredBlockIndex == tradableMaterial.RequiredBlockIndex);
                    }
                    else
                    {
                        return false;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static OrderDigest GetSellDigest(Guid tradableId,
            long requiredBlockIndex,
            FungibleAssetValue price,
            int count)
        {
            return _sellDigests.FirstOrDefault(x =>
                x.TradableId.Equals(tradableId) &&
                x.ExpiredBlockIndex.Equals(requiredBlockIndex) &&
                x.Price.Equals(price) &&
                x.ItemCount.Equals(count));
        }

        private const int buyItemsPerPage = 24;
        private const int sellItemsPerPage = 20;

        public static void InitAndUpdateBuyDigests()
        {
            _buyDigests = GetBuyOrderDigests();
            UpdateBuyDigests();
        }

        public static void InitSellDigests()
        {
            if (_sellDigests != null)
            {
                _sellDigests = GetSellOrderDigests();
            }
        }

        public static void InitAndUpdateSellDigests()
        {
            _sellDigests = GetSellOrderDigests();
            UpdateSellDigests();
        }

        private static void UpdateBuyDigests()
        {
            var buyDigests = _buyDigests.Where(digest =>
                !digest.SellerAgentAddress.Equals(States.Instance.AgentState.address)).ToList();
            BuyDigests.Value =
                GetGroupedOrderDigestsByItemSubTypeFilter(buyDigests, buyItemsPerPage);
        }

        private static void UpdateSellDigests()
        {
            SellDigests.Value =
                GetGroupedOrderDigestsByItemSubTypeFilter(_sellDigests, sellItemsPerPage);
        }

        public static void RemoveBuyDigest(Guid orderId)
        {
            var item = _buyDigests.FirstOrDefault(x => x.OrderId.Equals(orderId));
            if (item != null)
            {
                _buyDigests.Remove(item);
            }

            UpdateBuyDigests();
        }

        public static void RemoveSellDigest(Guid orderId)
        {
            var item = _sellDigests.FirstOrDefault(x => x.OrderId.Equals(orderId));
            if (item != null)
            {
                _sellDigests.Remove(item);
            }

            UpdateSellDigests();
        }

        private static
            Dictionary<ItemSubTypeFilter,
                Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>
            GetGroupedOrderDigestsByItemSubTypeFilter(IReadOnlyCollection<OrderDigest> digests,
                int shopItemsPerPage)
        {
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

            var groupedOrderDigests =
                new
                    Dictionary<ItemSubTypeFilter,
                        Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>>
                    {
                        { ItemSubTypeFilter.All, GetGroupedOrderDigestsBySortFilter(digests, shopItemsPerPage) },
                        { ItemSubTypeFilter.Weapon, GetGroupedOrderDigestsBySortFilter(weapons, shopItemsPerPage) },
                        { ItemSubTypeFilter.Armor, GetGroupedOrderDigestsBySortFilter(armors, shopItemsPerPage) },
                        { ItemSubTypeFilter.Belt, GetGroupedOrderDigestsBySortFilter(belts, shopItemsPerPage) },
                        { ItemSubTypeFilter.Necklace, GetGroupedOrderDigestsBySortFilter(necklaces, shopItemsPerPage) },
                        { ItemSubTypeFilter.Ring, GetGroupedOrderDigestsBySortFilter(rings, shopItemsPerPage) },
                        { ItemSubTypeFilter.Food_HP, GetGroupedOrderDigestsBySortFilter(foodsHp, shopItemsPerPage) },
                        { ItemSubTypeFilter.Food_ATK, GetGroupedOrderDigestsBySortFilter(foodsAtk, shopItemsPerPage) },
                        { ItemSubTypeFilter.Food_DEF, GetGroupedOrderDigestsBySortFilter(foodsDef, shopItemsPerPage) },
                        { ItemSubTypeFilter.Food_CRI, GetGroupedOrderDigestsBySortFilter(foodsCri, shopItemsPerPage) },
                        { ItemSubTypeFilter.Food_HIT, GetGroupedOrderDigestsBySortFilter(foodsHit, shopItemsPerPage) },
                        { ItemSubTypeFilter.FullCostume, GetGroupedOrderDigestsBySortFilter(fullCostumes, shopItemsPerPage) },
                        { ItemSubTypeFilter.HairCostume, GetGroupedOrderDigestsBySortFilter(hairCostumes, shopItemsPerPage) },
                        { ItemSubTypeFilter.EarCostume, GetGroupedOrderDigestsBySortFilter(earCostumes, shopItemsPerPage) },
                        { ItemSubTypeFilter.EyeCostume, GetGroupedOrderDigestsBySortFilter(eyeCostumes, shopItemsPerPage) },
                        { ItemSubTypeFilter.TailCostume, GetGroupedOrderDigestsBySortFilter(tailCostumes, shopItemsPerPage) },
                        { ItemSubTypeFilter.Title, GetGroupedOrderDigestsBySortFilter(titles, shopItemsPerPage) },
                        { ItemSubTypeFilter.Materials, GetGroupedOrderDigestsBySortFilter(materials, shopItemsPerPage) },
                    };
            return groupedOrderDigests;
        }

        private static Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>
            GetGroupedOrderDigestsBySortFilter(IReadOnlyCollection<OrderDigest> digests,
                int shopItemsPerPage)
        {
            return new Dictionary<ShopSortFilter, Dictionary<int, List<OrderDigest>>>
            {
                {
                    ShopSortFilter.Class,
                    GetGroupedShopItemsByPage(GetSortedOrderDigests(digests, SortType.Grade),
                        shopItemsPerPage)
                },
                {
                    ShopSortFilter.CP,
                    GetGroupedShopItemsByPage(GetSortedOrderDigests(digests, SortType.Cp),
                        shopItemsPerPage)
                },
                {
                    ShopSortFilter.Price,
                    GetGroupedShopItemsByPage(
                        digests.OrderByDescending(digest => digest.Price).ToList(),
                        shopItemsPerPage)
                },
            };
        }

        private static List<OrderDigest> GetSortedOrderDigests(IEnumerable<OrderDigest> digests,
            SortType type)
        {
            var result = new List<OrderDigest>();
            var costumeSheet = Game.Game.instance.TableSheets.CostumeItemSheet;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var costumes = new List<OrderDigest>();
            var materials = new List<OrderDigest>();
            var itemUsables = new List<OrderDigest>();

            foreach (var digest in digests)
            {
                if (costumeSheet.ContainsKey(digest.ItemId))
                {
                    costumes.Add(digest);
                }
                else if (materialSheet.ContainsKey(digest.ItemId))
                {
                    materials.Add(digest);
                }
                else
                {
                    itemUsables.Add(digest);
                }
            }

            result.AddRange(costumes.OrderByDescending(digest => GetTypeValue(digest, type)));
            result.AddRange(itemUsables.OrderByDescending(digest => GetTypeValue(digest, type)));
            result.AddRange(materials.OrderByDescending(digest => GetTypeValue(digest, type)));
            return result;
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

        private static Dictionary<int, List<OrderDigest>> GetGroupedShopItemsByPage(
            List<OrderDigest> digests,
            int shopItemsPerPage)
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

        private static List<OrderDigest> GetBuyOrderDigests()
        {
            var orderDigests = new Dictionary<Address, List<OrderDigest>>();

            foreach (var itemSubType in ItemSubTypes)
            {
                if (ShardedSubTypes.Contains(itemSubType))
                {
                    foreach (var addressKey in ShardedShopState.AddressKeys)
                    {
                        var address = ShardedShopStateV2.DeriveAddress(itemSubType, addressKey);
                        AddOrderDigest(address, orderDigests);
                    }
                }
                else
                {
                    var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                    AddOrderDigest(address, orderDigests);
                }
            }

            var digests = new List<OrderDigest>();
            foreach (var items in orderDigests.Select(i => i.Value))
            {
                digests.AddRange(items);
            }

            return digests;
        }

        private static void AddOrderDigest(Address address,
            IDictionary<Address, List<OrderDigest>> orderDigests)
        {
            var shardedShopState = Game.Game.instance.Agent.GetState(address);
            if (shardedShopState is Dictionary dictionary)
            {
                var state = new ShardedShopStateV2(dictionary);
                foreach (var orderDigest in state.OrderDigestList)
                {
                    if (orderDigest.ExpiredBlockIndex != 0 && orderDigest.ExpiredBlockIndex >
                        Game.Game.instance.Agent.BlockIndex)
                    {
                        var agentAddress = orderDigest.SellerAgentAddress;
                        if (!orderDigests.ContainsKey(agentAddress))
                        {
                            orderDigests.Add(agentAddress, new List<OrderDigest>());
                        }

                        orderDigests[agentAddress].Add(orderDigest);
                    }
                }
            }
        }

        private static List<OrderDigest> GetSellOrderDigests()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var receiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            var receiptState = Game.Game.instance.Agent.GetState(receiptAddress);
            var receipts = new List<OrderDigest>();
            if (receiptState is Dictionary dictionary)
            {
                var state = new OrderDigestListState(dictionary);
                receipts.AddRange(state.OrderDigestList);
            }

            return receipts;
        }
    }
}
