using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib9c.Model.Order;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    public static class ReactiveShopState
    {
        public static ReactiveProperty<List<ItemProductResponseModel>> BuyItemProducts { get; } =
            new();

        public static ReactiveProperty<List<ItemProductResponseModel>> SellItemProducts { get; } =
            new();

        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>>
            BuyFungibleAssetProducts { get; } = new();

        public static ReactiveProperty<List<FungibleAssetValueProductResponseModel>>
            SellFungibleAssetProducts { get; } = new();

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>>
            CachedBuyItemProducts = new()
            {
                { MarketOrderType.cp, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.cp_desc, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.price, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.grade, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.grade_desc, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.crystal, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.crystal_desc, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.crystal_per_price, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() },
                { MarketOrderType.crystal_per_price_desc, new Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>() }
            };
        private static readonly List<ItemProductResponseModel> CachedSellItemProducts = new();

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubTypeFilter, List<FungibleAssetValueProductResponseModel>>>
            CachedBuyFungibleAssetProducts = new()
            {
                { MarketOrderType.price, new Dictionary<ItemSubTypeFilter, List<FungibleAssetValueProductResponseModel>>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubTypeFilter, List<FungibleAssetValueProductResponseModel>>() },
            };
        private static readonly List<FungibleAssetValueProductResponseModel> CachedSellFungibleAssetProducts = new();

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubTypeFilter, bool>> BuyProductMaxChecker = new()
            {
                { MarketOrderType.cp, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.cp_desc, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.price, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.price_desc, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.grade, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.grade_desc, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.crystal, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.crystal_desc, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.crystal_per_price, new Dictionary<ItemSubTypeFilter, bool>() },
                { MarketOrderType.crystal_per_price_desc, new Dictionary<ItemSubTypeFilter, bool>() },
            };

        private static readonly Dictionary<MarketOrderType, Dictionary<ItemSubTypeFilter, bool>> BuyFavMaxChecker = new ()
        {
            { MarketOrderType.price, new Dictionary<ItemSubTypeFilter, bool>
            {
                { ItemSubTypeFilter.RuneStone, false}, { ItemSubTypeFilter.PetSoulStone, false}
            }},
            { MarketOrderType.price_desc, new Dictionary<ItemSubTypeFilter, bool>
            {
                { ItemSubTypeFilter.RuneStone, false}, { ItemSubTypeFilter.PetSoulStone, false}
            }},
        };

        private static List<Guid> PurchasedProductIds = new();

        public static void ClearCache()
        {
            BuyItemProducts.Value = new List<ItemProductResponseModel>();
            SellItemProducts.Value = new List<ItemProductResponseModel>();
            BuyFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();
            SellFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();

            foreach (var v in CachedBuyItemProducts.Values)
            {
                v.Clear();
            }
            foreach (var v in CachedBuyFungibleAssetProducts.Values)
            {
                v.Clear();
            }
            foreach (var v in BuyProductMaxChecker.Values)
            {
                v.Clear();
            }
            foreach (var v in BuyFavMaxChecker.Values)
            {
                v.Clear();
            }

            CachedSellItemProducts.Clear();
            CachedSellFungibleAssetProducts.Clear();
        }

        public static async Task RequestBuyProductsAsync(
            ItemSubTypeFilter filter,
            MarketOrderType orderType,
            int limit)
        {
            if (Game.Game.instance.MarketServiceClient is null)
            {
                return;
            }

            if (!BuyProductMaxChecker[orderType].ContainsKey(filter))
            {
                BuyProductMaxChecker[orderType].Add(filter, false);
            }

            if (BuyProductMaxChecker[orderType][filter])
            {
                return;
            }

            if (!CachedBuyItemProducts[orderType].ContainsKey(filter))
            {
                CachedBuyItemProducts[orderType]
                    .Add(filter, new List<ItemProductResponseModel>());
            }

            var offset = CachedBuyItemProducts[orderType][filter].Count;
            var itemSubType = filter.ToItemSubType();
            var statType = filter.ToItemStatType();
            var (products, totalCount) = await Game.Game.instance.MarketServiceClient.GetBuyProducts(itemSubType, offset, limit, orderType, statType);
            Debug.Log($"[RequestBuyProductsAsync] : {itemSubType} / {filter} / {orderType} / {offset} / {limit} / {statType} / MAX:{totalCount}");

            var productModels = CachedBuyItemProducts[orderType][filter];
            foreach (var product in products)
            {
                if (productModels.All(x => x.ProductId != product.ProductId))
                {
                    CachedBuyItemProducts[orderType][filter].Add(product);
                }
            }

            BuyProductMaxChecker[orderType][filter] = totalCount == CachedBuyItemProducts[orderType][filter].Count;
            SetBuyProducts(orderType);
        }

        public static async Task RequestBuyFungibleAssetsAsync(
            ItemSubTypeFilter filter,
            MarketOrderType orderType,
            int limit)
        {
            if (!BuyFavMaxChecker[orderType].ContainsKey(filter))
            {
                BuyFavMaxChecker[orderType].Add(filter, false);
            }

            if (BuyFavMaxChecker[orderType][filter])
            {
                return;
            }

            if (!CachedBuyFungibleAssetProducts[orderType].ContainsKey(filter))
            {
                CachedBuyFungibleAssetProducts[orderType]
                    .Add(filter, new List<FungibleAssetValueProductResponseModel>());
            }

            var ticker = filter == ItemSubTypeFilter.RuneStone ? "RUNE" : "Soulstone";
            var offset = CachedBuyFungibleAssetProducts[orderType][filter].Count;
            var (fungibleAssets, totalCount) =
                await Game.Game.instance.MarketServiceClient.GetBuyFungibleAssetProducts(ticker, offset, limit, orderType);
            Debug.Log($"[RequestBuyFungibleAssetsAsync] : {ticker} / {filter} / {orderType} / {offset} / {limit} / MAX:{totalCount}");
            var fungibleAssetModels = CachedBuyFungibleAssetProducts[orderType][filter];
            foreach (var asset in fungibleAssets)
            {
                if (fungibleAssetModels.All(x => x.ProductId != asset.ProductId))
                {
                    CachedBuyFungibleAssetProducts[orderType][filter].Add(asset);
                }
            }

            BuyFavMaxChecker[orderType][filter] = totalCount == CachedBuyFungibleAssetProducts[orderType][filter].Count;
            SetBuyFungibleAssets(orderType);
        }

        public static async Task RequestSellProductsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var (fungibleAssets, items) =
                await Game.Game.instance.MarketServiceClient.GetProducts(avatarAddress);
            CachedSellItemProducts.Clear();
            CachedSellItemProducts.AddRange(items);

            CachedSellFungibleAssetProducts.Clear();
            CachedSellFungibleAssetProducts.AddRange(fungibleAssets);
            SetSellProducts();
        }

        public static void SetBuyProducts(MarketOrderType marketOrderType)
        {
            var products = new List<ItemProductResponseModel>();
            var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
            foreach (var model in CachedBuyItemProducts[marketOrderType].Values.SelectMany(models => models))
            {
                if (model.Legacy)
                {
                    if (model.RegisteredBlockIndex + Order.ExpirationInterval - curBlockIndex > 0)
                    {
                        products.Add(model);
                    }
                }
                else
                {
                    products.Add(model);
                }
            }

            var agentAddress = States.Instance.AgentState.address;
            var buyProducts = products
                .Where(x => !x.SellerAgentAddress.Equals(agentAddress))
                .Where(x => !PurchasedProductIds.Contains(x.ProductId))
                .ToList();
            BuyItemProducts.Value = buyProducts;
        }

        public static void SetBuyFungibleAssets(MarketOrderType marketOrderType)
        {
            var fav = new List<FungibleAssetValueProductResponseModel>();
            foreach (var models in CachedBuyFungibleAssetProducts[marketOrderType].Values)
            {
                fav.AddRange(models);
            }

            var agentAddress = States.Instance.AgentState.address;
            var favProducts = fav
                .Where(x => !x.SellerAgentAddress.Equals(agentAddress))
                .Where(x => !PurchasedProductIds.Contains(x.ProductId))
                .ToList();
            BuyFungibleAssetProducts.Value = favProducts;
        }

        public static void SetSellProducts()
        {
            SellItemProducts.Value = CachedSellItemProducts;
            SellFungibleAssetProducts.Value = CachedSellFungibleAssetProducts;
        }

        public static void UpdatePurchaseProductIds(IEnumerable<Guid> ids)
        {
            foreach (var guid in ids.Where(guid => !PurchasedProductIds.Contains(guid)))
            {
                PurchasedProductIds.Add(guid);
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

        public static FungibleAssetValueProductResponseModel GetSellFungibleAssetProduct(
            Guid productId)
        {
            return SellFungibleAssetProducts.Value.FirstOrDefault(x => x.ProductId == productId);
        }

        public static bool TryGetItemBase(ItemProductResponseModel product, out ItemBase itemBase)
        {
            var itemRow = Game.Game.instance.TableSheets.ItemSheet[product.ItemId];
            var id = product.TradableId;
            var requiredBlockIndex = product.RegisteredBlockIndex + Order.ExpirationInterval;
            var madeWithMimisbrunnrRecipe = false;
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

            itemBase = (ItemBase)tradableItem;
            return itemBase is not null;
        }

        public static int GetCachedBuyItemCount(MarketOrderType orderType, ItemSubTypeFilter filter)
        {
            switch (filter)
            {
                case ItemSubTypeFilter.RuneStone:
                case ItemSubTypeFilter.PetSoulStone:
                    if (!CachedBuyFungibleAssetProducts.ContainsKey(orderType))
                    {
                        return 0;
                    }

                    if (!CachedBuyFungibleAssetProducts[orderType].ContainsKey(filter))
                    {
                        return 0;
                    }

                    return CachedBuyFungibleAssetProducts[orderType][filter].Count;

                default:
                    if (!CachedBuyItemProducts.ContainsKey(orderType))
                    {
                        return 0;
                    }

                    if (!CachedBuyItemProducts[orderType].ContainsKey(filter))
                    {
                        return 0;
                    }

                    var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    var legacyItemCount = CachedBuyItemProducts[orderType][filter]
                        .Where(x => x.Legacy)
                        .Count(x => x.RegisteredBlockIndex + Order.ExpirationInterval - curBlockIndex > 0);
                    var newItemCount = CachedBuyItemProducts[orderType][filter].Count(x=> !x.Legacy);
                    var sum = legacyItemCount + newItemCount;
                    return sum;
            }
        }
    }
}
