using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    /// <summary>
    /// Changes in the values included in ShopState are notified to the outside through each ReactiveProperty<T> field.
    /// </summary>
    public static class ReactiveShopState
    {
        private static readonly List<ItemSubType> ItemSubTypes = new()
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

        private static readonly List<ItemSubType> ShardedSubTypes = new()
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

        // public static ReactiveProperty<List<OrderDigest>> BuyDigest { get; } = new();

        public static ReactiveProperty<List<ItemProductModel>> BuyProducts { get; } = new();

        public static ReactiveProperty<List<ItemProductModel>> SellProducts { get; } = new();

        // key: orderId
        private static ConcurrentDictionary<Guid, ItemBase> CachedShopItems { get; } = new();

        private static ConcurrentBag<ItemProductModel> CachedShopProducts { get; } = new();
        private static readonly Dictionary<ItemSubType, List<ItemProductModel>> _buyDigest = new();
        private static List<Guid> _removedOrderIds { get; } = new();

        public static ItemProductModel GetSellProduct(
            Guid tradableId,
            long requiredBlockIndex,
            FungibleAssetValue price,
            int count)
        {
            return SellProducts.Value.FirstOrDefault(x =>
                x.TradableId.Equals(tradableId) &&
                // x.ExpiredBlockIndex.Equals(requiredBlockIndex) &&
                x.Price.Equals(price) &&
                x.Quantity.Equals(count));
        }

        public static bool TryGetShopItem(ItemProductModel orderDigest, out ItemBase itemBase)
        {
            if (!CachedShopItems.ContainsKey(orderDigest.ProductId))
            {
                Debug.LogWarning($"[{nameof(TryGetShopItem)}] Not found address:" +
                                 $" {orderDigest.ProductId}");
                itemBase = null;
                return false;
            }

            itemBase = CachedShopItems[orderDigest.ProductId];
            return true;
        }

        public static async UniTask SetBuyProductsAsync(List<ItemSubType> list)
        {
            await UniTask.Run(async () =>
            {
                _removedOrderIds.Clear();

                if (!CachedShopProducts.Any())
                {
                    var products = await Game.Game.instance.MarketServiceClient.GetProducts();
                    foreach (var product in products)
                    {
                        CachedShopProducts.Add(product);
                    }
                }

                foreach (var itemSubType in list)
                {
                    var digests = GetBuyProductsFromQuery(itemSubType);
                    // var digests = await GetBuyOrderDigestsAsync(itemSubType);
                    var result = UpdateCachedShopItemsAsync(digests);
                    if (result)
                    {
                        AddBuyDigest(digests, itemSubType);
                    }
                }

                return true;
            });
        }

        private static void AddBuyDigest(List<ItemProductModel> digests, ItemSubType itemSubType)
        {
            var agentAddress = States.Instance.AgentState.address;
            var d = digests
                .Where(digest => !digest.SellerAgentAddress.Equals(agentAddress))
                .ToList();
            if (!_buyDigest.ContainsKey(itemSubType))
            {
                _buyDigest.Add(itemSubType, new List<ItemProductModel>());
            }

            _buyDigest[itemSubType] = d;

            var buyDigests = new List<ItemProductModel>();
            foreach (var pair in _buyDigest)
            {
                buyDigests.AddRange(pair.Value);
            }

            var removeList = buyDigests
                .Where(digest => _removedOrderIds.Contains(digest.ProductId))
                .ToList();
            foreach (var orderDigest in removeList)
            {
                buyDigests.Remove(orderDigest);
            }

            BuyProducts.Value = buyDigests;
        }

        public static async UniTask UpdateSellDigestsAsync()
        {
            var digests = await GetSellOrderDigestsAsync();
            // var result = await UpdateCachedShopItemsAsync(digests);
            // if (result)
            // {
            //     SellDigest.Value = digests;
            // }
        }

        public static void RemoveBuyDigest(Guid orderId)
        {
            var item = BuyProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(orderId));
            if (item != null)
            {
                if (!_removedOrderIds.Contains(orderId))
                {
                    _removedOrderIds.Add(orderId);
                }

                BuyProducts.Value.Remove(item);
                BuyProducts.SetValueAndForceNotify(BuyProducts.Value);
            }
        }

        public static void RemoveSellDigest(Guid orderId)
        {
            var item = SellProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(orderId));
            if (item != null)
            {
                SellProducts.Value.Remove(item);
                SellProducts.SetValueAndForceNotify(SellProducts.Value);
            }
        }

        private static async UniTask<List<OrderDigest>> GetBuyOrderDigestsAsync(
            ItemSubType itemSubType)
        {
            var orderDigests = new Dictionary<Address, List<OrderDigest>>();
            var addressList = new List<Address>();

            if (ShardedSubTypes.Contains(itemSubType))
            {
                addressList.AddRange(ShardedShopState.AddressKeys.Select(addressKey =>
                    ShardedShopStateV2.DeriveAddress(itemSubType, addressKey)));
            }
            else
            {
                var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                addressList.Add(address);
            }

            var values =
                await Game.Game.instance.Agent.GetStateBulk(addressList);
            var shopStates = new List<ShardedShopStateV2>();
            foreach (var kv in values)
            {
                if (kv.Value is Dictionary shopDict)
                {
                    shopStates.Add(new ShardedShopStateV2(shopDict));
                }
            }

            AddProducts(shopStates, orderDigests);

            var digests = new List<OrderDigest>();
            foreach (var items in orderDigests.Values)
            {
                digests.AddRange(items);
            }

            return digests;
        }

        private static List<ItemProductModel> GetBuyProductsFromQuery(ItemSubType itemSubType)
        {
            var orderDigests = new Dictionary<Address, List<ItemProductModel>>();
            AddProducts(orderDigests, itemSubType);
            var digests = new List<ItemProductModel>();
            foreach (var items in orderDigests.Values)
            {
                digests.AddRange(items);
            }

            return digests;
        }
        private static void AddProducts(
            List<ShardedShopStateV2> shopStates,
            IDictionary<Address, List<OrderDigest>> orderDigests)
        {
            foreach (var shopState in shopStates)
            {
                foreach (var orderDigest in shopState.OrderDigestList)
                {
                    if (orderDigest.ExpiredBlockIndex != 0 &&
                        orderDigest.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
                    {
                        var agentAddress = orderDigest.SellerAgentAddress;
                        if (!orderDigests.ContainsKey(agentAddress))
                        {
                            orderDigests[agentAddress] = new List<OrderDigest>();
                        }

                        orderDigests[agentAddress].Add(orderDigest);
                    }
                }
            }
        }

        private static void AddProducts(IDictionary<Address, List<ItemProductModel>> products, ItemSubType itemSubType)
        {
            var cachedDigests = CachedShopProducts.Where(i => i.ItemSubType.Equals(itemSubType)).Select(s => s).ToList();
            foreach (var product in cachedDigests)
            {
                if (!products.ContainsKey(product.SellerAgentAddress))
                {
                    products[product.SellerAgentAddress] = new List<ItemProductModel>();
                }

                products[product.SellerAgentAddress].Add(product);
            }
        }

        private static async UniTask<List<OrderDigest>> GetSellOrderDigestsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var receiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            var receiptState = await Game.Game.instance.Agent.GetStateAsync(receiptAddress);
            var receipts = new List<OrderDigest>();
            if (receiptState is Dictionary dictionary)
            {
                var state = new OrderDigestListState(dictionary);
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var validOrderDigests = state.OrderDigestList
                    .Where(x => x.ExpiredBlockIndex > currentBlockIndex);
                receipts.AddRange(validOrderDigests);

                var expiredOrderDigests = state.OrderDigestList
                    .Where(x => x.ExpiredBlockIndex <= currentBlockIndex);
                var inventory = States.Instance.CurrentAvatarState.inventory;
                var lockedDigests = expiredOrderDigests
                    .Where(x => inventory.TryGetLockedItem(new OrderLock(x.OrderId), out _))
                    .ToList();
                receipts.AddRange(lockedDigests);
            }

            return receipts;
        }

        private static bool UpdateCachedShopItemsAsync(
            IEnumerable<ItemProductModel> products)
        {
            var selectedProducts = products
                .Where(product => !CachedShopItems.ContainsKey(product.ProductId))
                .ToList();
            Debug.Log($"CachedShopItems: {CachedShopItems.Count}");
            // var tuples = selectedDigests
            //     .Select(e => (Address: Addresses.GetItemAddress(e.TradableId), OrderDigest: e))
            //     .ToArray();
            // var itemAddresses = tuples
            //     .Select(tuple => tuple.Address)
            //     .Distinct();
            // var itemValues =
            //     await Game.Game.instance.Agent.GetStateBulk(itemAddresses);
            foreach (var product in selectedProducts)
            {
                // if (!itemValues.ContainsKey(address))
                // {
                //     Debug.LogWarning($"[{nameof(ReactiveShopState)}] Not found address:" +
                //                      $" {address.ToHex()}");
                //     continue;
                // }
                //
                // var itemValue = itemValues[address];
                // if (!(itemValue is Dictionary dictionary))
                // {
                //     Debug.LogWarning($"[{nameof(ReactiveShopState)}] {nameof(itemValue)}" +
                //                      $" cannot cast to {typeof(Bencodex.Types.Dictionary).FullName}");
                //     continue;
                // }
                //
                // var itemBase = ItemFactory.Deserialize(dictionary);
                // switch (itemBase)
                // {
                //     case TradableMaterial tm:
                //         tm.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                //         break;
                //     case ItemUsable iu:
                //         iu.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                //         break;
                //     case Costume c:
                //         c.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                //         break;
                // }
                var itemRow = Game.Game.instance.TableSheets.ItemSheet[product.ItemId];
                var id = product.TradableId;
                long requiredBlockIndex = 0L;
                bool madeWithMimisbrunnrRecipe = false;
                ItemUsable itemUsable = null;
                switch (itemRow.ItemSubType)
                {
                    // Consumable
                    case ItemSubType.Food:
                        itemUsable = new Consumable((ConsumableItemSheet.Row) itemRow, id, requiredBlockIndex);
                        break;
                    // Equipment
                    case ItemSubType.Weapon:
                        itemUsable = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Armor:
                        itemUsable = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Belt:
                        itemUsable = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Necklace:
                        itemUsable = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Ring:
                        itemUsable = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                }

                foreach (var skillModel in product.Skills)
                {
                    var skillRow = Game.Game.instance.TableSheets.SkillSheet[skillModel.SkillId];
                    var skill = SkillFactory.Get(skillRow, skillModel.Power, skillModel.Chance);
                    itemUsable.Skills.Add(skill);
                }

                foreach (var statModel in product.Stats)
                {
                    if (statModel.Additional)
                    {
                        itemUsable.StatsMap.AddStatAdditionalValue(statModel.Type, statModel.Value);
                    }
                    else
                    {
                        var current = itemUsable.StatsMap.GetBaseStats(true).First(r => r.statType == statModel.Type).baseValue;
                        itemUsable.StatsMap.AddStatValue(statModel.Type, statModel.Value - current);
                    }
                }

                if (itemUsable is Equipment equipment)
                {
                    equipment.level = product.Level;
                }
                CachedShopItems.TryAdd(product.ProductId, itemUsable);
            }

            return true;
        }
    }
}
