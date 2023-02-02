using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Game;
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
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            return SellProducts.Value.FirstOrDefault(x =>
                x.TradableId.Equals(tradableId) &&
                (x.RegisteredBlockIndex + Order.ExpirationInterval).Equals(requiredBlockIndex) &&
                ((BigInteger)x.Price * currency).Equals(price) &&
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

                foreach (var itemSubType in list)
                {
                    var products = await Game.Game.instance.MarketServiceClient.GetProducts(itemSubType);
                    foreach (var product in products)
                    {
                        CachedShopProducts.Add(product);
                    }

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
            var digests = await GetSellProductsAsync();
            var result = UpdateCachedShopItemsAsync(digests);
            if (result)
            {
                SellProducts.Value = digests;
            }
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
                if (product.RegisteredBlockIndex > 0)
                {
                    if (!products.ContainsKey(product.SellerAgentAddress))
                    {
                        products[product.SellerAgentAddress] = new List<ItemProductModel>();
                    }

                    products[product.SellerAgentAddress].Add(product);
                }
            }
        }

        private static async UniTask<List<ItemProductModel>> GetSellProductsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var receipts = new List<ItemProductModel>();
            var products = await Game.Game.instance.MarketServiceClient.GetProducts(avatarAddress);
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var validOrderDigests = products
                .Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval > currentBlockIndex);
            receipts.AddRange(validOrderDigests);

            var expiredOrderDigests = products
                .Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval <= currentBlockIndex);
            receipts.AddRange(expiredOrderDigests);

            return receipts;
        }

        private static bool UpdateCachedShopItemsAsync(
            IEnumerable<ItemProductModel> products)
        {
            var selectedProducts = products
                .Where(product => !CachedShopItems.ContainsKey(product.ProductId))
                .ToList();
            Debug.Log($"CachedShopItems: {CachedShopItems.Count}");
            foreach (var product in selectedProducts)
            {
                var itemRow = Game.Game.instance.TableSheets.ItemSheet[product.ItemId];
                var id = product.TradableId;
                long requiredBlockIndex = product.RegisteredBlockIndex + Order.ExpirationInterval;
                bool madeWithMimisbrunnrRecipe = false;
                ITradableItem tradableItem = null;
                switch (itemRow.ItemSubType)
                {
                    // Consumable
                    case ItemSubType.Food:
                        tradableItem = new Consumable((ConsumableItemSheet.Row) itemRow, id, requiredBlockIndex);
                        break;
                    // Equipment
                    case ItemSubType.Weapon:
                        tradableItem = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Armor:
                        tradableItem = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Belt:
                        tradableItem = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Necklace:
                        tradableItem = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Ring:
                        tradableItem = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.ApStone:
                    case ItemSubType.Hourglass:
                        tradableItem = new TradableMaterial((MaterialItemSheet.Row)itemRow);
                        break;
                    case ItemSubType.EarCostume:
                    case ItemSubType.EyeCostume:
                    case ItemSubType.FullCostume:
                    case ItemSubType.HairCostume:
                    case ItemSubType.TailCostume:
                    case ItemSubType.Title:
                        tradableItem =  new Costume((CostumeItemSheet.Row)itemRow, id);
                        break;
                }

                if (tradableItem is ItemUsable itemUsable)
                {
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

                }
                if (tradableItem is Equipment equipment)
                {
                    equipment.level = product.Level;
                }
                CachedShopItems.TryAdd(product.ProductId, (ItemBase)tradableItem);
            }

            return true;
        }
    }
}
