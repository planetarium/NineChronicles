using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lib9c.Model.Order;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;

namespace Nekoyume.State
{
    public static class ReactiveShopState
    {
        const string RuneStone = "RUNE";
        const string PetSoulStone = "SOULSTONE";
        const int PageSize = 15;

        public static ItemFilterOptions ItemFilterOptions { get; private set; }

        private static readonly List<int> ItemIds = new();
        private static readonly List<int> RuneIds = new();
        private static readonly List<int> PetIds = new();

        public static bool IsNeedSearch => !string.IsNullOrWhiteSpace(ItemFilterOptions.SearchText);
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
            int[] itemIds = null)
        {
            if (!reset && BuyProductMaxChecker)
            {
                return;
            }

            var itemSubType = filter.ToItemSubType();
            var statType = filter.ToItemStatType();
            var offset = reset ? 0 : CachedBuyItemProducts.Count;
            var (products, totalCount) =
                await Game.Game.instance.MarketServiceClient.GetBuyProducts(itemSubType, offset, limit, orderType, statType, itemIds);

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
                await Game.Game.instance.MarketServiceClient.GetBuyFungibleAssetProducts(tickers, offset, limit, orderType);

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
                await Game.Game.instance.MarketServiceClient.GetProducts(avatarAddress);

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
                    tradableItem = weapon;
                    break;
                case ItemSubType.Armor:
                    var armor = new Armor((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    armor.optionCountFromCombination = product.OptionCountFromCombination;
                    tradableItem = armor;
                    break;
                case ItemSubType.Belt:
                    var belt = new Belt((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    belt.optionCountFromCombination = product.OptionCountFromCombination;
                    tradableItem = belt;
                    break;
                case ItemSubType.Necklace:
                    var necklace = new Necklace((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    necklace.optionCountFromCombination = product.OptionCountFromCombination;
                    tradableItem = necklace;
                    break;
                case ItemSubType.Ring:
                    var ring = new Ring((EquipmentItemSheet.Row)itemRow, id,
                        requiredBlockIndex, madeWithMimisbrunnrRecipe);
                    ring.optionCountFromCombination = product.OptionCountFromCombination;
                    tradableItem = ring;
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

        public static void Initialize()
        {
            var tableSheets = Game.Game.instance.TableSheets;

            ItemIds.AddRange(tableSheets.EquipmentItemSheet.Values.Select(x => x.Id));
            ItemIds.AddRange(tableSheets.ConsumableItemSheet.Values.Select(x => x.Id));
            ItemIds.AddRange(tableSheets.CostumeItemSheet.Values.Select(x => x.Id));
            ItemIds.AddRange(tableSheets.MaterialItemSheet.Values.Select(x => x.Id));
            RuneIds.AddRange(tableSheets.RuneListSheet.Values.Select(x=> x.Id));
            PetIds.AddRange(tableSheets.PetSheet.Values.Select(x=> x.Id));
        }

        #region ItemFilter
        public static void SetItemFilterOption(ItemFilterOptions type)
        {
            ItemFilterOptions = type;
        }

        public static async Task RefreshItemsAsync(ItemSubTypeFilter filter, MarketOrderType orderType, int itemCountPerPage, bool useLevelLimit, bool isReset)
        {
            if (filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
            {
                var tickers = GetFilteredTicker(filter);
                await RequestBuyFungibleAssetsAsync(tickers, orderType, itemCountPerPage * PageSize, isReset);
            }
            else
            {
                var filteredItemIds = GetFilteredItemIds(filter, useLevelLimit);
                await RequestBuyProductsAsync(filter, orderType, itemCountPerPage * PageSize, isReset, filteredItemIds);
            }
        }

        private static int[] GetFilteredItemIds(ItemSubTypeFilter filter, bool useLevelLimit)
        {
            var avatarLevel = Game.Game.instance.States.CurrentAvatarState.level;
            var requirementSheet = Game.Game.instance.TableSheets.ItemRequirementSheet;

            if ((!IsNeedSearch && !useLevelLimit) ||
                filter is ItemSubTypeFilter.RuneStone or ItemSubTypeFilter.PetSoulStone)
            {
                return Array.Empty<int>();
            }

            bool IsValid(int id)
            {
                var inSearch = !IsNeedSearch ||
                               Regex.IsMatch(L10nManager.LocalizeItemName(id), ItemFilterOptions.SearchText, RegexOptions.IgnoreCase);
                var inLevelLimit = !useLevelLimit ||
                                   (requirementSheet.TryGetValue(id, out var requirementRow) &&
                                    avatarLevel >= requirementRow.Level);
                return inSearch && inLevelLimit;
            }

            return ItemIds.Where(IsValid).ToArray();
        }

        private static string[] GetFilteredTicker(ItemSubTypeFilter filter)
        {
            if (!IsNeedSearch)
            {
                return new[] { filter == ItemSubTypeFilter.RuneStone ? RuneStone : PetSoulStone };
            }

            var itemName = ItemFilterOptions.SearchText;
            switch (filter)
            {
                case ItemSubTypeFilter.RuneStone:
                    var filteredRuneList = RuneIds.Where(id => Regex.IsMatch(L10nManager.LocalizeRuneName(id), itemName, RegexOptions.IgnoreCase)).ToList();
                    var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                    return filteredRuneList.Any()
                        ? filteredRuneList.Select(id => runeSheet[id].Ticker).ToArray()
                        : new[] { RuneStone };

                case ItemSubTypeFilter.PetSoulStone:
                    var filteredPetList = PetIds.Where(id => Regex.IsMatch(L10nManager.LocalizePetName(id), itemName, RegexOptions.IgnoreCase)).ToList();
                    var petSheet = Game.Game.instance.TableSheets.PetSheet;
                    return filteredPetList.Any()
                        ? filteredPetList.Select(id => petSheet[id].SoulStoneTicker.ToUpper()).ToArray()
                        : new[] { PetSoulStone };
                default:
                    return new[] { RuneStone };
            }
        }

        public static void ResetItemFilter()
        {
            ItemFilterOptions = new ItemFilterOptions();
        }
        #endregion ItemFilter
    }
}
