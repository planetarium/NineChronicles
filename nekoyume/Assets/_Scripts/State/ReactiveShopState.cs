using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lib9c.Model.Order;
using MarketService.Response;
using Nekoyume.ApiClient;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using UniRx;

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

        private static readonly List<ItemProductResponseModel> CachedBuyItemProducts = new();
        private static readonly List<ItemProductResponseModel> CachedSellItemProducts = new();

        private static readonly List<FungibleAssetValueProductResponseModel> CachedBuyFungibleAssetProducts = new();
        private static readonly List<FungibleAssetValueProductResponseModel> CachedSellFungibleAssetProducts = new();

        private static bool BuyProductMaxChecker;
        private static bool BuyFavMaxChecker;

        private static List<Guid> PurchasedProductIds = new();

        public static void ClearCache()
        {
            BuyItemProducts.Value = new List<ItemProductResponseModel>();
            SellItemProducts.Value = new List<ItemProductResponseModel>();
            BuyFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();
            SellFungibleAssetProducts.Value = new List<FungibleAssetValueProductResponseModel>();

            CachedBuyItemProducts.Clear();
            CachedSellItemProducts.Clear();
            CachedBuyFungibleAssetProducts.Clear();
            CachedSellFungibleAssetProducts.Clear();

            BuyProductMaxChecker = false;
            BuyFavMaxChecker = false;
        }

        public static async Task RequestBuyProductsAsync(
            ItemSubTypeFilter filter,
            MarketOrderType orderType,
            int limit,
            bool reset = false,
            int[] ids = null,
            bool isCustom = false)
        {
            if (!reset && BuyProductMaxChecker)
            {
                return;
            }

            var itemSubType = filter.ToItemSubType();
            var statType = filter.ToItemStatType();
            var offset = reset ? 0 : CachedBuyItemProducts.Count;
            var (products, totalCount) =
                await ApiClients.Instance.MarketServiceClient.GetBuyProducts(itemSubType, offset, limit, orderType, statType, ids, isCustom);

            if (reset)
            {
                CachedBuyItemProducts.Clear();
                CachedBuyFungibleAssetProducts.Clear();
            }

            foreach (var product in products)
            {
                if (CachedBuyItemProducts.All(x => x.ProductId != product.ProductId))
                {
                    CachedBuyItemProducts.Add(product);
                }
            }

            BuyProductMaxChecker = totalCount == CachedBuyItemProducts.Count;
            SetBuyProducts();
        }

        public static async Task RequestBuyFungibleAssetsAsync(
            string[] tickers,
            MarketOrderType orderType,
            int limit,
            bool reset = true)
        {
            if (!reset && BuyFavMaxChecker)
            {
                return;
            }

            var offset = reset ? 0 : CachedBuyFungibleAssetProducts.Count;
            var (fungibleAssets, totalCount) =
                await ApiClients.Instance.MarketServiceClient.GetBuyFungibleAssetProducts(tickers, offset, limit, orderType);

            if (reset)
            {
                CachedBuyItemProducts.Clear();
                CachedBuyFungibleAssetProducts.Clear();
            }

            foreach (var asset in fungibleAssets)
            {
                if (CachedBuyFungibleAssetProducts.All(x => x.ProductId != asset.ProductId))
                {
                    CachedBuyFungibleAssetProducts.Add(asset);
                }
            }

            BuyFavMaxChecker = totalCount == CachedBuyFungibleAssetProducts.Count;
            SetBuyFungibleAssets();
        }

        public static async Task RequestSellProductsAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var (fungibleAssets, items) =
                await ApiClients.Instance.MarketServiceClient.GetProducts(avatarAddress);

            CachedSellItemProducts.Clear();
            CachedSellItemProducts.AddRange(items);

            CachedSellFungibleAssetProducts.Clear();
            CachedSellFungibleAssetProducts.AddRange(fungibleAssets);

            SetSellProducts();
        }

        public static void SetBuyProducts()
        {
            var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var agentAddress = States.Instance.AgentState.address;

            var products = new List<ItemProductResponseModel>();
            foreach (var model in CachedBuyItemProducts.Where(model =>
                model.SellerAgentAddress != agentAddress &&
                !PurchasedProductIds.Contains(model.ProductId)))
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

            BuyItemProducts.Value = products;
            BuyFungibleAssetProducts.Value?.Clear();
        }

        public static void SetBuyFungibleAssets()
        {
            var agentAddress = States.Instance.AgentState.address;

            var favProducts = CachedBuyFungibleAssetProducts.Where(x =>
                !x.SellerAgentAddress.Equals(agentAddress) &&
                !PurchasedProductIds.Contains(x.ProductId)).ToList();

            BuyFungibleAssetProducts.Value = favProducts;
            BuyItemProducts.Value?.Clear();
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

        public static ItemProductResponseModel GetSellItemProduct(Guid productId)
        {
            return SellItemProducts.Value
                .FirstOrDefault(model => model is not null && model.ProductId == productId);
        }

        public static FungibleAssetValueProductResponseModel GetSellFungibleAssetProduct(Guid productId)
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
                    var weapon = new Weapon((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    weapon.optionCountFromCombination = product.OptionCountFromCombination;
                    weapon.IconId = product.IconId;
                    weapon.ByCustomCraft = product.ByCustomCraft;
                    tradableItem = weapon;
                    break;
                case ItemSubType.Armor:
                    var armor = new Armor((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    armor.optionCountFromCombination = product.OptionCountFromCombination;
                    armor.IconId = product.IconId;
                    armor.ByCustomCraft = product.ByCustomCraft;
                    tradableItem = armor;
                    break;
                case ItemSubType.Belt:
                    var belt = new Belt((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    belt.optionCountFromCombination = product.OptionCountFromCombination;
                    belt.IconId = product.IconId;
                    belt.ByCustomCraft = product.ByCustomCraft;
                    tradableItem = belt;
                    break;
                case ItemSubType.Necklace:
                    var necklace = new Necklace((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    necklace.optionCountFromCombination = product.OptionCountFromCombination;
                    necklace.IconId = product.IconId;
                    necklace.ByCustomCraft = product.ByCustomCraft;
                    tradableItem = necklace;
                    break;
                case ItemSubType.Ring:
                    var ring = new Ring((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    ring.optionCountFromCombination = product.OptionCountFromCombination;
                    ring.IconId = product.IconId;
                    ring.ByCustomCraft = product.ByCustomCraft;
                    tradableItem = ring;
                    break;
                case ItemSubType.ApStone:
                case ItemSubType.Hourglass:
                case ItemSubType.Scroll:
                case ItemSubType.Circle:
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
                    var skill = SkillFactory.Get(
                        skillRow,
                        skillModel.Power,
                        skillModel.Chance,
                        skillModel.StatPowerRatio,
                        skillModel.ReferencedStatType);
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

        public static int GetCachedBuyItemCount(ItemSubTypeFilter filter)
        {
            switch (filter)
            {
                case ItemSubTypeFilter.RuneStone:
                case ItemSubTypeFilter.PetSoulStone:
                    return CachedBuyFungibleAssetProducts.Count;
                default:
                    var curBlockIndex = Game.Game.instance.Agent.BlockIndex;
                    var legacyItemCount = CachedBuyItemProducts
                        .Where(x => x.Legacy)
                        .Count(x => x.RegisteredBlockIndex + Order.ExpirationInterval - curBlockIndex > 0);
                    var newItemCount = CachedBuyItemProducts.Count(x => !x.Legacy);
                    var sum = legacyItemCount + newItemCount;
                    return sum;
            }
        }
    }
}
