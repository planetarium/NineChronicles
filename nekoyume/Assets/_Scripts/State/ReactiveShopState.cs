using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet.Assets;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    public static class ReactiveShopState
    {
        public static ReactiveProperty<List<ItemProductResponseModel>> BuyItemProducts { get; } = new();
        public static ReactiveProperty<List<ItemProductResponseModel>> SellItemProducts { get; } = new();

        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>> BuyFungibleAssetProducts { get; } = new();
        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>> SellFungibleAssetProducts { get; } = new();
        private static Dictionary<Guid, ItemBase> CachedBuyItems { get; set; } = new();
        private static Dictionary<Guid, ItemBase> CachedSellItems { get; set; } = new();

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubType, List<ItemProductResponseModel>>>
            CachedBuyProducts = new()
            {
                { MarketOrderType.cp, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
                { MarketOrderType.cp_desc, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
                { MarketOrderType.price, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
                { MarketOrderType.grade, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
                { MarketOrderType.grade_desc, new Dictionary<ItemSubType, List<ItemProductResponseModel>>() },
            };

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubType, bool>> MarketMaxChecker = new()
            {
                { MarketOrderType.cp, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.cp_desc, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.price, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.grade, new Dictionary<ItemSubType, bool>() },
                { MarketOrderType.grade_desc, new Dictionary<ItemSubType, bool>() },
            };

        private static readonly List<ItemProductResponseModel> CachedSellItemProducts = new();
        private static readonly List<FungibleAssetValueProductResponseModel> CachedSellFungibleAssetProducts = new();

        private static List<Guid> RemovedProductIds => new();

        public static bool TryGetBuyShopItem(Guid productId, out ItemBase itemBase)
        {
            if (!CachedBuyItems.ContainsKey(productId))
            {
                Debug.LogWarning($"[{nameof(TryGetBuyShopItem)}] Not found address:" +
                                 $" {productId}");
                itemBase = null;
                return false;
            }

            itemBase = CachedBuyItems[productId];
            return true;
        }

        public static bool TryGetSellShopItem(Guid productId, out ItemBase itemBase)
        {
            if (!CachedSellItems.ContainsKey(productId))
            {
                Debug.LogWarning($"[{nameof(TryGetSellShopItem)}] Not found address:" +
                                 $" {productId}");
                itemBase = null;
                return false;
            }

            itemBase = CachedSellItems[productId];
            return true;
        }

        public static void ClearCache()
        {
            BuyItemProducts.Value = new List<ItemProductResponseModel>();
            SellItemProducts.Value = new List<ItemProductResponseModel>();
            BuyFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();
            SellFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();
            CachedBuyItems?.Clear();
            CachedSellItems?.Clear();
            RemovedProductIds?.Clear();

            foreach (var v in CachedBuyProducts.Values)
            {
                v.Clear();
            }

            foreach (var v in MarketMaxChecker.Values)
            {
                v.Clear();
            }
        }

        public static async Task RequestBuyProductsAsync(
            ItemSubType itemSubType,
            MarketOrderType orderType,
            int offset,
            int limit)
        {
            if (Game.Game.instance.MarketServiceClient is null)
            {
                return;
            }

            if (!MarketMaxChecker[orderType].ContainsKey(itemSubType))
            {
                MarketMaxChecker[orderType].Add(itemSubType, false);
            }

            if (MarketMaxChecker[orderType][itemSubType])
            {
                Debug.Log("[RequestBuyProductsAsync] MAX");
                return;
            }

            var (products, totalCount) =
                await Game.Game.instance.MarketServiceClient.GetBuyProducts(
                    itemSubType, offset, limit, orderType);

            Debug.Log(
                $"[RequestBuyProductsAsync] : {itemSubType} / {orderType} / {offset} / {limit} / MAX:{totalCount}");

            var count = GetCachedBuyItemCount(orderType, itemSubType);
            MarketMaxChecker[orderType][itemSubType] = count == totalCount;

            RemovedProductIds.Clear();
            if (!CachedBuyProducts[orderType].ContainsKey(itemSubType))
            {
                CachedBuyProducts[orderType].Add(itemSubType, new List<ItemProductResponseModel>());
            }

            var productModels = CachedBuyProducts[orderType][itemSubType];
            foreach (var product in products)
            {
                if (productModels.All(x => x.ProductId != product.ProductId))
                {
                    CachedBuyProducts[orderType][itemSubType].Add(product);
                }
            }

            SetBuyProducts(orderType);
        }

        public static async Task RequestSellProductsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var (fungibleAssets, items) = await Game.Game.instance.MarketServiceClient.GetProducts(avatarAddress);
            CachedSellItemProducts.Clear();
             CachedSellItemProducts.AddRange(items);
            CachedSellFungibleAssetProducts.Clear();
            CachedSellFungibleAssetProducts.AddRange(fungibleAssets);
            SetSellProducts();
        }

        public static void SetBuyProducts(MarketOrderType marketOrderType)
        {
            var products = new List<ItemProductResponseModel>();
            foreach (var models in CachedBuyProducts[marketOrderType].Values)
            {
                products.AddRange(models.Where(model => model.RegisteredBlockIndex > 0));
            }

            BuyItemProducts.Value = SortedProducts(products);
            BuyFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>(); // todo
            CachedBuyItems = GetItems(BuyItemProducts.Value);
        }

        public static void SetSellProducts()
        {
            SellItemProducts.Value = CachedSellItemProducts;
            SellFungibleAssetProducts.Value = CachedSellFungibleAssetProducts;
            CachedSellItems = GetItems(SellItemProducts.Value);
        }

        private static List<ItemProductResponseModel> SortedProducts(
            IReadOnlyCollection<ItemProductResponseModel> products)
        {
            var agentAddress = States.Instance.AgentState.address;
            var buyProducts = products
                .Where(product => !product.SellerAgentAddress.Equals(agentAddress))
                .ToList();

            var removeList = products
                .Where(product => RemovedProductIds.Contains(product.ProductId))
                .ToList();

            foreach (var model in removeList)
            {
                buyProducts.Remove(model);
            }

            return buyProducts;
        }

        public static void RemoveBuyProduct(Guid productId)
        {
            var item = BuyItemProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (item != null)
            {
                if (!RemovedProductIds.Contains(productId))
                {
                    RemovedProductIds.Add(productId);
                }

                BuyItemProducts.Value.Remove(item);
                BuyItemProducts.SetValueAndForceNotify(BuyItemProducts.Value);
            }
        }

        public static void RemoveSellProduct(Guid productId)
        {
            var itemProduct = SellItemProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (itemProduct is not null)
            {
                SellItemProducts.Value.Remove(itemProduct);
                SellItemProducts.SetValueAndForceNotify(SellItemProducts.Value);
            }

            var fungibleAssetProduct = SellFungibleAssetProducts.Value.FirstOrDefault(x =>
                x.ProductId.Equals(productId));
            if (fungibleAssetProduct is not null)
            {
                SellFungibleAssetProducts.Value.Remove(fungibleAssetProduct);
                SellFungibleAssetProducts.SetValueAndForceNotify(SellFungibleAssetProducts.Value);
            }
        }

        public static ItemProductResponseModel GetSellItemProduct(Guid productId)
        {
            return SellItemProducts.Value.FirstOrDefault(x => x.ProductId == productId);
        }

        public static FungibleAssetValueProductResponseModel GetSellFungibleAssetProduct(Guid productId)
        {
            return SellFungibleAssetProducts.Value.FirstOrDefault(x => x.ProductId == productId);
        }

        private static Dictionary<Guid, ItemBase> GetItems(
            IEnumerable<ItemProductResponseModel> products)
        {
            var result = new Dictionary<Guid, ItemBase>();
            foreach (var product in products)
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
                        tradableItem = new Consumable((ConsumableItemSheet.Row)itemRow, id,
                            requiredBlockIndex);
                        break;
                    // Equipment
                    case ItemSubType.Weapon:
                        tradableItem = new Weapon((EquipmentItemSheet.Row)itemRow, id,
                            requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Armor:
                        tradableItem = new Armor((EquipmentItemSheet.Row)itemRow, id,
                            requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Belt:
                        tradableItem = new Belt((EquipmentItemSheet.Row)itemRow, id,
                            requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Necklace:
                        tradableItem = new Necklace((EquipmentItemSheet.Row)itemRow, id,
                            requiredBlockIndex, madeWithMimisbrunnrRecipe);
                        break;
                    case ItemSubType.Ring:
                        tradableItem = new Ring((EquipmentItemSheet.Row)itemRow, id,
                            requiredBlockIndex, madeWithMimisbrunnrRecipe);
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
                        tradableItem = new Costume((CostumeItemSheet.Row)itemRow, id);
                        break;
                }

                if (tradableItem is ItemUsable itemUsable)
                {
                    foreach (var skillModel in product.SkillModels)
                    {
                        var skillRow =
                            Game.Game.instance.TableSheets.SkillSheet[skillModel.SkillId];
                        var skill = SkillFactory.Get(skillRow, skillModel.Power, skillModel.Chance);
                        itemUsable.Skills.Add(skill);
                    }

                    foreach (var statModel in product.StatModels)
                    {
                        if (statModel.Additional)
                        {
                            itemUsable.StatsMap.AddStatAdditionalValue(statModel.Type,
                                statModel.Value);
                        }
                        else
                        {
                            var current = itemUsable.StatsMap.GetBaseStats(true)
                                .First(r => r.statType == statModel.Type).baseValue;
                            itemUsable.StatsMap.AddStatValue(statModel.Type,
                                statModel.Value - current);
                        }
                    }
                }

                if (tradableItem is Equipment equipment)
                {
                    equipment.level = product.Level;
                }

                result.TryAdd(product.ProductId, (ItemBase)tradableItem);
            }

            return result;
        }

        public static int GetCachedBuyItemCount(MarketOrderType orderType, ItemSubType itemSubType)
        {
            if (!CachedBuyProducts.ContainsKey(orderType))
                return 0;

            if (!CachedBuyProducts[orderType].ContainsKey(itemSubType))
                return 0;

            return CachedBuyProducts[orderType][itemSubType].Count;
        }
    }
}
