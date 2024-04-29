using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Model;
using UnityEngine;
using MailModel = Nekoyume.Model.Mail.Mail;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume
{
    public static class LocalizationExtensions
    {
        // FIXME: Move to MailExtensions.
        public static async Task<string> ToInfo(this MailModel mail)
        {
            switch (mail)
            {
                case CombinationMail combinationMail:
                {
                    string formatKey;
                    if (combinationMail.attachment.itemUsable is Equipment equipment)
                    {
                        if (combinationMail.attachment is CombinationConsumable5.ResultModel
                                result &&
                            result.subRecipeId.HasValue)
                        {
                            if (TableSheets.Instance.EquipmentItemSubRecipeSheetV2 is null)
                            {
                                if (TableSheets.Instance.EquipmentItemSubRecipeSheet.TryGetValue(
                                        result.subRecipeId.Value,
                                        out var row))
                                {
                                    formatKey = equipment.optionCountFromCombination ==
                                                row.Options.Count
                                        ? "UI_COMBINATION_NOTIFY_FORMAT_GREATER"
                                        : "UI_COMBINATION_NOTIFY_FORMAT";
                                }
                                else
                                {
                                    formatKey = "UI_COMBINATION_NOTIFY_FORMAT";
                                }
                            }
                            else if (TableSheets.Instance.EquipmentItemSubRecipeSheetV2.TryGetValue(
                                         result.subRecipeId.Value,
                                         out var row))
                            {
                                formatKey = equipment.optionCountFromCombination ==
                                            row.Options.Count
                                    ? "UI_COMBINATION_NOTIFY_FORMAT_GREATER"
                                    : "UI_COMBINATION_NOTIFY_FORMAT";
                            }
                            else
                            {
                                formatKey = "UI_COMBINATION_NOTIFY_FORMAT";
                            }
                        }
                        else
                        {
                            formatKey = "UI_COMBINATION_NOTIFY_FORMAT";
                        }
                    }
                    else
                    {
                        formatKey = "UI_COMBINATION_NOTIFY_FORMAT";
                    }

                    return L10nManager.Localize(formatKey, GetLocalizedNonColoredName(
                        combinationMail.attachment.itemUsable,
                        combinationMail.attachment.itemUsable.ItemType.HasElementType()));
                }

                case MaterialCraftMail materialCraftMail:
                    var materialItemName =
                        L10nManager.Localize($"ITEM_NAME_{materialCraftMail.ItemId}");
                    return L10nManager.Localize("UI_COMBINATION_NOTIFY_FORMAT", materialItemName);

                case ItemEnhanceMail itemEnhanceMail:
                {
                    string formatKey;
                    bool failAndGainCrystal = false;
                    switch (itemEnhanceMail.attachment)
                    {
                        case ItemEnhancement13.ResultModel result:
                            switch (result.enhancementResult)
                            {
                                case ItemEnhancement13.EnhancementResult.Success:
                                    formatKey = "UI_ITEM_ENHANCEMENT_MAIL_FORMAT";
                                    break;
                                default:
                                    NcDebug.LogError(
                                        $"Unexpected result.enhancementResult: {result.enhancementResult}");
                                    formatKey = "UI_ITEM_ENHANCEMENT_MAIL_FORMAT";
                                    break;
                            }

                            break;
                        case ItemEnhancement7.ResultModel _:
                        case ItemEnhancement9.ResultModel _:
                        case ItemEnhancement10.ResultModel _:
                        case ItemEnhancement11.ResultModel _:
                        case ItemEnhancement12.ResultModel _:
                                formatKey = "UI_ITEM_ENHANCEMENT_MAIL_FORMAT";
                            break;
                        default:
                            NcDebug.LogError(
                                "itemEnhanceMail.attachment is not ItemEnhancement.ResultModel");
                            formatKey = "UI_ITEM_ENHANCEMENT_MAIL_FORMAT";
                            break;
                    }

                    if (failAndGainCrystal)
                    {
                        return L10nManager.Localize(formatKey,
                            GetLocalizedNonColoredName(itemEnhanceMail.attachment.itemUsable),
                            ((ItemEnhancement13.ResultModel)itemEnhanceMail.attachment).CRYSTAL);
                    }

                    return L10nManager.Localize(formatKey,
                        GetLocalizedNonColoredName(itemEnhanceMail.attachment.itemUsable));
                }

                case OrderBuyerMail orderBuyerMail:
                    var buyerItemName =
                        await Util.GetItemNameByOrderId(orderBuyerMail.OrderId, true);
                    return L10nManager.Localize("UI_BUYER_MAIL_FORMAT", buyerItemName);

                case OrderSellerMail orderSellerMail:
                    var order = await Util.GetOrder(orderSellerMail.OrderId);
                    var sellerItemName =
                        await Util.GetItemNameByOrderId(orderSellerMail.OrderId, true);
                    var taxedPrice = order.Price - order.GetTax();
                    return L10nManager.Localize("UI_SELLER_MAIL_FORMAT", taxedPrice,
                        sellerItemName);

                case OrderExpirationMail orderExpirationMail:
                    var expiredItemName =
                        await Util.GetItemNameByOrderId(orderExpirationMail.OrderId, true);
                    return L10nManager.Localize("UI_SELL_EXPIRATION_MAIL_FORMAT", expiredItemName);

                case CancelOrderMail cancelOrderMail:
                    var cancelItemName =
                        await Util.GetItemNameByOrderId(cancelOrderMail.OrderId, true);
                    return L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT", cancelItemName);

                case BuyerMail buyerMail:
                    var buyerMailItemBase = GetItemBase(buyerMail.attachment);
                    return L10nManager.Localize("UI_BUYER_MAIL_FORMAT",
                        GetLocalizedNonColoredName(buyerMailItemBase,
                            buyerMailItemBase.ItemType.HasElementType()));

                case SellerMail sellerMail:
                    var attachment = sellerMail.attachment;
                    if (!(attachment is Buy7.SellerResult sellerResult))
                    {
                        throw new InvalidCastException(
                            $"({nameof(Buy7.SellerResult)}){nameof(attachment)}");
                    }

                    return L10nManager.Localize("UI_SELLER_MAIL_FORMAT",
                        sellerResult.gold,
                        GetLocalizedNonColoredName(GetItemBase(attachment),
                            attachment.itemUsable.ItemType.HasElementType()));

                case SellCancelMail sellCancelMail:
                    var cancelMailItemBase = GetItemBase(sellCancelMail.attachment);
                    return L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT",
                        GetLocalizedNonColoredName(cancelMailItemBase,
                            cancelMailItemBase.ItemType.HasElementType()));
                case DailyRewardMail _:
                    return L10nManager.Localize("UI_DAILY_REWARD_MAIL_FORMAT");
                case MonsterCollectionMail _:
                    return L10nManager.Localize("UI_MONSTER_COLLECTION_MAIL_FORMAT");
                case GrindingMail grindingMail:
                    return L10nManager.Localize("UI_GRINDING_CRYSTALMAIL_FORMAT",
                        grindingMail.Asset.ToString());
                case RaidRewardMail rewardMail:
                    return L10nManager.Localize("UI_RAID_SEASON_REWARD_MAIL_FORMAT",
                        rewardMail.RaidId, rewardMail.CurrencyName, rewardMail.Amount);

                case ProductCancelMail productCancelMail:
                    var (productName, _, _) =
                        await Game.Game.instance.MarketServiceClient.GetProductInfo(
                            productCancelMail
                                .ProductId);
                    return L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT", productName);
                case ProductBuyerMail productBuyerMail:
                    var (buyProductName, _, _) =
                        await Game.Game.instance.MarketServiceClient.GetProductInfo(productBuyerMail
                            .ProductId);
                    return L10nManager.Localize("UI_BUYER_MAIL_FORMAT", buyProductName);
                case ProductSellerMail productSellerMail:
                    var (sellProductName, item, fav) =
                        await Game.Game.instance.MarketServiceClient.GetProductInfo(
                            productSellerMail.ProductId);
                    var price = item?.Price ?? fav?.Price ?? 0;
                    var tax = decimal.Divide(price, 100) * Buy.TaxRate;
                    var tp = price - tax;
                    var currency = States.Instance.GoldBalanceState.Gold.Currency;
                    var majorUnit = (int)tp;
                    var minorUnit = (int)((tp - majorUnit) * 100);
                    var fungibleAsset = new FungibleAssetValue(currency, majorUnit, minorUnit);
                    return L10nManager.Localize("UI_SELLER_MAIL_FORMAT", fungibleAsset,
                        sellProductName);
                case UnloadFromMyGaragesRecipientMail unloadFromMyGaragesRecipientMail:
                    return await unloadFromMyGaragesRecipientMail.GetCellContentAsync();
                case ClaimItemsMail claimItemsMail:
                    return await claimItemsMail.GetCellContentAsync();
                default:
                    throw new NotSupportedException(
                        $"Given mail[{mail}] doesn't support {nameof(ToInfo)}() method.");
            }
        }

        private static ItemBase GetItemBase(AttachmentActionResult attachment)
        {
            if (attachment.itemUsable != null)
            {
                return attachment.itemUsable;
            }

            if (attachment.costume != null)
            {
                return attachment.costume;
            }

            return (ItemBase)attachment.tradableFungibleItem;
        }

        public static string GetTitle(this QuestModel quest)
        {
            switch (quest)
            {
                case CollectQuest _:
                case CombinationQuest _:
                case ItemEnhancementQuest _:
                case CombinationEquipmentQuest _:
                    return L10nManager.Localize("QUEST_TITLE_CRAFT");
                case GeneralQuest generalQuest:
                    string key;
                    switch (generalQuest.Event)
                    {
                        case QuestEventType.Create:
                        case QuestEventType.Level:
                        case QuestEventType.Die:
                        case QuestEventType.Complete:
                            key = "ADVENTURE";
                            break;
                        case QuestEventType.Enhancement:
                        case QuestEventType.Equipment:
                        case QuestEventType.Consumable:
                            key = "CRAFT";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return L10nManager.Localize($"QUEST_TITLE_{key}");
                case ItemGradeQuest _:
                case ItemTypeCollectQuest _:
                case MonsterQuest _:
                    return L10nManager.Localize("QUEST_TITLE_ADVENTURE");
                case GoldQuest _:
                case TradeQuest _:
                    return L10nManager.Localize("QUEST_TITLE_TRADE");
                case WorldQuest _:
                    return L10nManager.Localize("QUEST_TITLE_ADVENTURE");
                default:
                    throw new NotSupportedException(
                        $"Given quest[{quest}] doesn't support {nameof(GetTitle)}() method.");
            }
        }

        public static string GetContent(this QuestModel quest)
        {
            switch (quest)
            {
                case CollectQuest collectQuest:
                    return L10nManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT",
                        L10nManager.LocalizeItemName(collectQuest.ItemId));
                case CombinationQuest combinationQuest:
                    return L10nManager.Localize("QUEST_COMBINATION_CURRENT_INFO_FORMAT",
                        combinationQuest.ItemSubType.GetLocalizedString(), combinationQuest.Goal);
                case GeneralQuest generalQuest:
                    switch (generalQuest.Event)
                    {
                        case QuestEventType.Create:
                            break;
                        case QuestEventType.Enhancement:
                        case QuestEventType.Level:
                        case QuestEventType.Die:
                        case QuestEventType.Complete:
                        case QuestEventType.Equipment:
                        case QuestEventType.Consumable:
                            return L10nManager.Localize(
                                $"QUEST_GENERAL_{generalQuest.Event}_FORMAT",
                                generalQuest.Goal);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return L10nManager.Localize($"QUEST_GENERAL_{generalQuest.Event}_FORMAT");
                case GoldQuest goldQuest:
                    return
                        L10nManager.Localize(
                            $"QUEST_GOLD_{goldQuest.Type}_FORMAT",
                            goldQuest.Goal);
                case ItemEnhancementQuest itemEnhancementQuest:
                    return L10nManager.Localize(
                        "QUEST_ITEM_ENHANCEMENT_FORMAT",
                        itemEnhancementQuest.Grade,
                        itemEnhancementQuest.Goal,
                        itemEnhancementQuest.Count);
                case ItemGradeQuest itemGradeQuest:
                    return L10nManager.Localize(
                        "QUEST_ITEM_GRADE_FORMAT",
                        itemGradeQuest.Grade,
                        itemGradeQuest.Goal);
                case ItemTypeCollectQuest itemTypeCollectQuest:
                    return L10nManager.Localize(
                        "QUEST_ITEM_TYPE_FORMAT",
                        itemTypeCollectQuest.ItemType.GetLocalizedString(),
                        itemTypeCollectQuest.Goal);
                case MonsterQuest monsterQuest:
                    return L10nManager.Localize(
                        "QUEST_MONSTER_FORMAT",
                        L10nManager.LocalizeCharacterName(monsterQuest.MonsterId));
                case TradeQuest tradeQuest:
                    return L10nManager.Localize(
                        "QUEST_TRADE_CURRENT_INFO_FORMAT",
                        tradeQuest.Type.GetLocalizedString(),
                        tradeQuest.Goal);
                case WorldQuest worldQuest:
                    if (TableSheets.Instance.WorldSheet.TryGetByStageId(
                            worldQuest.Goal,
                            out var worldRow))
                    {
                        if (worldQuest.Goal == worldRow.StageBegin)
                        {
                            return L10nManager.Localize(
                                "QUEST_WORLD_FORMAT",
                                worldRow.GetLocalizedName());
                        }

                        return L10nManager.Localize(
                            "QUEST_CLEAR_STAGE_FORMAT",
                            worldRow.GetLocalizedName(),
                            worldQuest.Goal);
                    }

                    if (!TableSheets.Instance.EventDungeonSheet.TryGetRowByEventDungeonStageId(
                            worldQuest.Goal,
                            out var eventDungeonRow))
                    {
                        return string.Empty;
                    }

                    if (worldQuest.Goal == eventDungeonRow.StageBegin)
                    {
                        return L10nManager.Localize(
                            "QUEST_WORLD_FORMAT",
                            eventDungeonRow.GetLocalizedName());
                    }

                    return L10nManager.Localize(
                        "QUEST_CLEAR_STAGE_FORMAT",
                        eventDungeonRow.GetLocalizedName(),
                        worldQuest.Goal.ToEventDungeonStageNumber());
                case CombinationEquipmentQuest combinationEquipmentQuest:
                    if (TableSheets.Instance.EquipmentItemRecipeSheet
                        .TryGetValue(combinationEquipmentQuest.RecipeId, out var recipeRow))
                    {
                        var itemRow = recipeRow.GetResultEquipmentItemRow();
                        return L10nManager.Localize(
                            "QUEST_COMBINATION_EQUIPMENT_FORMAT",
                            itemRow.GetLocalizedName(false));
                    }

                    if (TableSheets.Instance.EventScheduleSheet
                        .TryGetValue(combinationEquipmentQuest.RecipeId, out var eventScheduleRow))
                    {
                        return L10nManager.Localize(
                            "QUEST_COMBINATION_EQUIPMENT_FORMAT",
                            L10nManager.Localize("UI_EVENT_ITEM"));
                    }

                    return string.Empty;
                default:
                    throw new NotSupportedException(
                        $"Given quest[{quest}] doesn't support {nameof(GetContent)}() method.");
            }
        }

        public static string GetLocalizedString(this ItemType value)
        {
            return L10nManager.Localize($"ITEM_TYPE_{value}");
        }

        public static string GetLocalizedString(this ItemSubType value)
        {
            return L10nManager.Localize($"ITEM_SUB_TYPE_{value}");
        }

        public static string GetLocalizedString(this TradeType value)
        {
            return L10nManager.Localize($"TRADE_TYPE_{value}");
        }

        public static string GetLocalizedString(this StatType value)
        {
            return L10nManager.Localize($"STAT_TYPE_{value}");
        }

        public static string GetLocalizedString(this ElementalType value)
        {
            return L10nManager.Localize($"ELEMENTAL_TYPE_{value.ToString().ToUpper()}");
        }

        public static IEnumerable<string> GetOptions(this Player player)
        {
            var atkOptions = player.atkElementType.GetOptions(StatType.ATK);
            foreach (var atkOption in atkOptions)
            {
                yield return atkOption;
            }

            var defOptions = player.defElementType.GetOptions(StatType.DEF);
            foreach (var defOption in defOptions)
            {
                yield return defOption;
            }
        }

        public static string GetLocalizedName(
            this ItemBase item,
            bool useElementalIcon = true,
            bool ignoreLevel = false)
        {
            var name = item.GetLocalizedNonColoredName(useElementalIcon);
            switch (item)
            {
                case Equipment equipment:
                    return !ignoreLevel && equipment.level > 0
                        ? $"<color=#{GetColorHexByGrade(item)}>+{equipment.level} {name}</color>"
                        : $"<color=#{GetColorHexByGrade(item)}>{name}</color>";
                default:
                    return $"<color=#{GetColorHexByGrade(item)}>{name}</color>";
            }
        }

        public static string GetLocalizedName(this FungibleAssetValue fav)
        {
            return GetLocalizedFavName(fav.Currency.Ticker);
        }

        public static string GetLocalizedFavName(string ticker)
        {
            if(ticker.ToLower() == "crystal" || ticker.ToLower() == "fav_crystal")
            {
                return L10nManager.Localize($"ITEM_NAME_9999998");
            }

            var isRune = false;
            var id = 0;
            var grade = 1;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    isRune = true;
                    id = runeData.id;
                    grade = row.Grade;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                isRune = false;
                id = petRow.Id;
                grade = petRow.Grade;
            }

            var name = L10nManager.Localize(isRune ? $"RUNE_NAME_{id}" : $"PET_NAME_{id}");
            return $"<color=#{GetColorHexByGrade(grade)}>{name}</color>";
        }

        public static string GetLocalizedNonColoredName(this ItemBase item,
            bool useElementalIcon = true)
        {
            return GetLocalizedNonColoredName(
                item.ElementalType,
                item.Id,
                useElementalIcon && item.ItemType.HasElementType());
        }

        public static string GetLocalizedName(this EquipmentItemSheet.Row equipmentRow, int level,
            bool useElementalIcon = true)
        {
            var name = GetLocalizedNonColoredName(
                equipmentRow.ElementalType,
                equipmentRow.Id,
                useElementalIcon);

            return level > 0
                ? $"<color=#{GetColorHexByGrade(equipmentRow.Grade)}>+{level} {name}</color>"
                : $"<color=#{GetColorHexByGrade(equipmentRow.Grade)}>{name}</color>";
        }

        public static string GetLocalizedName(this ConsumableItemSheet.Row consumableRow,
            bool hasColor = true)
        {
            var name =
                GetLocalizedNonColoredName(consumableRow.ElementalType, consumableRow.Id, false);
            return hasColor
                ? $"<color=#{GetColorHexByGrade(consumableRow.Grade)}>{name}</color>"
                : name;
        }

        public static string GetLocalizedNonColoredName(ElementalType elementalType,
            int equipmentId, bool useElementalIcon)
        {
            var elemental = useElementalIcon ? GetElementalIcon(elementalType) : string.Empty;
            var name = L10nManager.Localize($"ITEM_NAME_{equipmentId}");
            return $"{name}{elemental}";
        }

        public static string GetLocalizedName(this SummonSheet.Row summonRow)
        {
            return L10nManager.Localize($"SUMMON_NAME_{summonRow.GroupId}");
        }

        public static string GetLocalizedInformation(this Equipment equipment)
        {
            var grade = equipment.GetGradeText();
            var subType = equipment.GetSubTypeText();
            return $"<color=#{GetColorHexByGrade(equipment.Grade)}>{grade}  |  {subType}</color>";
        }

        public static string GetLocalizedInformation(this FungibleAssetValue fav)
        {
            var grade = Util.GetTickerGrade(fav.Currency.Ticker);
            var gradeText = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
            var subType = L10nManager.Localize("UI_STONES");
            return $"<color=#{GetColorHexByGrade(grade)}>{gradeText}  |  {subType}</color>";
        }

        public static Color GetElementalTypeColor(this ItemBase item)
        {
            return GetElementalTypeColor(item.ElementalType);
        }

        public static Color GetElementalTypeColor(this ElementalType elementalType)
        {
            return elementalType switch
            {
                ElementalType.Normal => Palette.GetColor(EnumType.ColorType.TextElement00),
                ElementalType.Fire => Palette.GetColor(EnumType.ColorType.TextElement01),
                ElementalType.Land => Palette.GetColor(EnumType.ColorType.TextElement02),
                ElementalType.Water => Palette.GetColor(EnumType.ColorType.TextElement04),
                ElementalType.Wind => Palette.GetColor(EnumType.ColorType.TextElement05),
                _ => Color.white,
            };
        }

        public static Color GetItemGradeColor(this ItemBase item)
        {
            return GetItemGradeColor(item.Grade);
        }

        public static Color GetItemGradeColor(int grade)
        {
            return grade switch
            {
                1 => Palette.GetColor(EnumType.ColorType.TextGrade00),
                2 => Palette.GetColor(EnumType.ColorType.TextGrade01),
                3 => Palette.GetColor(EnumType.ColorType.TextGrade02),
                4 => Palette.GetColor(EnumType.ColorType.TextGrade03),
                5 => Palette.GetColor(EnumType.ColorType.TextGrade04),
                6 => Palette.GetColor(EnumType.ColorType.TextGrade05),
                _ => Palette.GetColor(EnumType.ColorType.TextGrade00),
            };
        }

        public static Color GetBuffGradeColor(this CrystalRandomBuffSheet.Row.BuffRank grade)
        {
            return grade switch
            {
                CrystalRandomBuffSheet.Row.BuffRank.SS =>
                    Palette.GetColor(EnumType.ColorType.TextGrade04),
                CrystalRandomBuffSheet.Row.BuffRank.S =>
                    Palette.GetColor(EnumType.ColorType.TextGrade03),
                CrystalRandomBuffSheet.Row.BuffRank.A =>
                    Palette.GetColor(EnumType.ColorType.TextGrade02),
                CrystalRandomBuffSheet.Row.BuffRank.B =>
                    Palette.GetColor(EnumType.ColorType.TextGrade00),
                _ => Palette.GetColor(EnumType.ColorType.TextGrade00),
            };
        }

        public static string ColorToHex(this Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);

            var result = $"{r:x2}{g:x2}{b:x2}";
            return result;
        }

        public static string GetLocalizedDescription(this ItemBase item)
        {
            return L10nManager.Localize($"ITEM_DESCRIPTION_{item.Id}");
        }

        public static string GetColorHexByGrade(int grade)
        {
            var color = GetItemGradeColor(grade);
            return color.ColorToHex();
        }

        public static string GetColorHexByGrade(this ItemBase item)
        {
            var color = GetColorHexByGrade(item.Grade);
            return color;
        }

        private static string GetElementalIcon(ElementalType type)
        {
            return type switch
            {
                ElementalType.Normal => "<sprite name=icon_Element_0>",
                ElementalType.Fire => "<sprite name=icon_Element_1>",
                ElementalType.Water => "<sprite name=icon_Element_2>",
                ElementalType.Land => "<sprite name=icon_Element_3>",
                ElementalType.Wind => "<sprite name=icon_Element_4>",
                _ => "<sprite name=icon_Element_0>"
            };
        }

        public static string GetLocalizedItemSubTypeText(ItemSubType type)
        {
            switch (type)
            {
                case ItemSubType.Title:
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                    return L10nManager.Localize("UI_COSTUME");
                case ItemSubType.ApStone:
                case ItemSubType.Food:
                    return L10nManager.Localize("UI_CONSUMABLE");
                case ItemSubType.Weapon:
                    return L10nManager.Localize("UI_WEAPON");
                case ItemSubType.Armor:
                    return L10nManager.Localize("UI_ARMOR");
                case ItemSubType.Belt:
                    return L10nManager.Localize("UI_BELT");
                case ItemSubType.Necklace:
                    return L10nManager.Localize("UI_NECKLACE");
                case ItemSubType.Ring:
                    return L10nManager.Localize("UI_RING");
                case ItemSubType.Aura:
                    return L10nManager.Localize("UI_AURA");
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                case ItemSubType.Hourglass:
                    return L10nManager.Localize("UI_MATERIAL");
                default:
                    return string.Empty;
            }
        }

        public static string GetGradeText(this ItemBase itemBase)
        {
            var grade = itemBase.Grade >= 1 ? itemBase.Grade : 1;
            var gradeText = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
            return gradeText;
        }

        public static string GetSubTypeText(this ItemBase itemBase)
        {
            var subTypeText = GetLocalizedItemSubTypeText(itemBase.ItemSubType);
            return subTypeText;
        }

        public static string GetPaymentFormatText(this FungibleAssetValue asset,
            string usageMessage,
            BigInteger cost)
        {
            // NCG
            if (asset.Currency.Equals(
                    Game.Game.instance.States.GoldBalanceState.Gold.Currency))
            {
                var ncgText = L10nManager.Localize("UI_NCG");
                return L10nManager.Localize(
                    "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                    cost,
                    ncgText,
                    usageMessage);
            }

            // CRYSTAL
            if (asset.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                var crystalText = L10nManager.Localize("UI_CRYSTAL");
                return L10nManager.Localize(
                    "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                    cost,
                    crystalText,
                    usageMessage);
            }

            NcDebug.LogWarning($"This Currency is not defined in 9c! {asset.Currency.ToString()}");
            return string.Empty;
        }

        public static string GetLocalizedName(this ItemSheet.Row row, int level)
        {
            var itemName = L10nManager.Localize($"ITEM_NAME_{row.Id}");
            if (level > 0)
            {
                itemName = $"+{level} {itemName}";
            }

            return $"<color=#{GetColorHexByGrade(row.Grade)}>{itemName}</color>";
        }

        public static (Color gradeColor, string gradeText, string subTypeText)
            GetGradeData(this ItemSheet.Row row)
        {
            var gradeColor = GetItemGradeColor(row.Grade);

            var grade = row.Grade >= 1 ? row.Grade : 1;
            var gradeText = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
            var subTypeText = GetLocalizedItemSubTypeText(row.ItemSubType);

            return (gradeColor, gradeText, subTypeText);
        }
    }
}
