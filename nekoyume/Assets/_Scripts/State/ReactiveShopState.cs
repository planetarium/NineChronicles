using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
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

        public static ReactiveProperty<List<ItemProductModel>> BuyProducts { get; } = new();

        public static ReactiveProperty<List<ItemProductModel>> SellProducts { get; } = new();

        // key: orderId
        private static ConcurrentDictionary<Guid, ItemBase> CachedShopItems { get; } = new();

        private static ConcurrentBag<ItemProductModel> CachedShopProducts { get; } = new();
        private static readonly Dictionary<ItemSubType, List<ItemProductModel>> _buyProduct = new();
        private static List<Guid> _removedProductIds { get; } = new();

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

        public static bool TryGetShopItem(ItemProductModel product, out ItemBase itemBase)
        {
            if (!CachedShopItems.ContainsKey(product.ProductId))
            {
                Debug.LogWarning($"[{nameof(TryGetShopItem)}] Not found address:" +
                                 $" {product.ProductId}");
                itemBase = null;
                return false;
            }

            itemBase = CachedShopItems[product.ProductId];
            return true;
        }

        public static async UniTask SetBuyProductsAsync(List<ItemSubType> list)
        {
            await UniTask.Run(async () =>
            {
                _removedProductIds.Clear();

                foreach (var itemSubType in list)
                {
                    var products = await Game.Game.instance.MarketServiceClient.GetProducts(itemSubType);
                    foreach (var product in products)
                    {
                        CachedShopProducts.Add(product);
                    }

                    var buyProducts = GetBuyProducts(itemSubType);
                    var result = UpdateCachedShopItemsAsync(buyProducts);
                    if (result)
                    {
                        AddBuyProduct(buyProducts, itemSubType);
                    }
                }

                return true;
            });
        }

        private static void AddBuyProduct(List<ItemProductModel> products, ItemSubType itemSubType)
        {
            var agentAddress = States.Instance.AgentState.address;
            var p = products
                .Where(product => !product.SellerAgentAddress.Equals(agentAddress))
                .ToList();
            if (!_buyProduct.ContainsKey(itemSubType))
            {
                _buyProduct.Add(itemSubType, new List<ItemProductModel>());
            }

            _buyProduct[itemSubType] = p;

            var buyProducts = new List<ItemProductModel>();
            foreach (var pair in _buyProduct)
            {
                buyProducts.AddRange(pair.Value);
            }

            var removeList = buyProducts
                .Where(product => _removedProductIds.Contains(product.ProductId))
                .ToList();
            foreach (var product in removeList)
            {
                buyProducts.Remove(product);
            }

            BuyProducts.Value = buyProducts;
        }

        public static async UniTask UpdateSellProductsAsync()
        {
            var products = await GetSellProductsAsync();
            var result = UpdateCachedShopItemsAsync(products);
            if (result)
            {
                SellProducts.Value = products;
            }
        }

        public static void RemoveBuyProduct(Guid productId)
        {
            var item = BuyProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (item != null)
            {
                if (!_removedProductIds.Contains(productId))
                {
                    _removedProductIds.Add(productId);
                }

                BuyProducts.Value.Remove(item);
                BuyProducts.SetValueAndForceNotify(BuyProducts.Value);
            }
        }

        public static void RemoveSellProduct(Guid productId)
        {
            var item = SellProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (item != null)
            {
                SellProducts.Value.Remove(item);
                SellProducts.SetValueAndForceNotify(SellProducts.Value);
            }
        }

        private static List<ItemProductModel> GetBuyProducts(ItemSubType itemSubType)
        {
            var productsDictionary = new Dictionary<Address, List<ItemProductModel>>();
            AddProducts(productsDictionary, itemSubType);
            var products = new List<ItemProductModel>();
            foreach (var items in productsDictionary.Values)
            {
                products.AddRange(items);
            }

            return products;
        }

        private static void AddProducts(IDictionary<Address, List<ItemProductModel>> products, ItemSubType itemSubType)
        {
            var cachedProducts = CachedShopProducts.Where(i => i.ItemSubType.Equals(itemSubType)).Select(s => s).ToList();
            foreach (var product in cachedProducts)
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
            var validProducts = products
                .Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval > currentBlockIndex);
            receipts.AddRange(validProducts);

            var expiredProducts = products
                .Where(x => x.RegisteredBlockIndex + Order.ExpirationInterval <= currentBlockIndex);
            receipts.AddRange(expiredProducts);

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
