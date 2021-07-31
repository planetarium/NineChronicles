using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
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
using UnityEngine;
using MailModel = Nekoyume.Model.Mail.Mail;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI
{
    public static class LocalizationExtension
    {
        public static string ToInfo(this MailModel mail)
        {
            switch (mail)
            {
                case CombinationMail combinationMail:
                    return string.Format(L10nManager.Localize("UI_COMBINATION_NOTIFY_FORMAT"),
                        GetLocalizedNonColoredName(combinationMail.attachment.itemUsable));

                case ItemEnhanceMail itemEnhanceMail:
                    return string.Format(L10nManager.Localize("UI_ITEM_ENHANCEMENT_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(itemEnhanceMail.attachment.itemUsable));

                case OrderBuyerMail orderBuyerMail:
                    var buyItem = Util.GetItemBaseByOrderId(orderBuyerMail.OrderId);
                    return string.Format(L10nManager.Localize("UI_BUYER_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(buyItem));

                case OrderSellerMail orderSellerMail:
                    var orderId = orderSellerMail.OrderId;
                    var order = Util.GetOrder(orderId);
                    var itembase = Util.GetItemBaseByTradableId(order.TradableId);
                    var format = L10nManager.Localize("UI_SELLER_MAIL_FORMAT");
                    var taxedPrice = order.Price - order.GetTax();
                    return string.Format(format, taxedPrice, GetLocalizedNonColoredName(itembase));

                case OrderExpirationMail orderExpirationMail:
                    var expiredItem = Util.GetItemBaseByOrderId(orderExpirationMail.OrderId);
                    return string.Format(L10nManager.Localize("UI_SELL_EXPIRATION_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(expiredItem));
                case CancelOrderMail cancelOrderMail:
                    var cancelItem = Util.GetItemBaseByOrderId(cancelOrderMail.OrderId);
                    return string.Format(L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(cancelItem));

                case BuyerMail buyerMail:
                    return string.Format(
                        L10nManager.Localize("UI_BUYER_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(GetItemBase(buyerMail.attachment)));

                case SellerMail sellerMail:
                    var attachment = sellerMail.attachment;
                    if (!(attachment is Buy7.SellerResult sellerResult))
                    {
                        throw new InvalidCastException($"({nameof(Buy7.SellerResult)}){nameof(attachment)}");
                    }
                    return string.Format(
                        L10nManager.Localize("UI_SELLER_MAIL_FORMAT"),
                        sellerResult.gold,
                        GetLocalizedNonColoredName(GetItemBase(attachment)));

                case SellCancelMail sellCancelMail:
                    return string.Format(
                        L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT"),
                        GetLocalizedNonColoredName(GetItemBase(sellCancelMail.attachment))
                    );

                case DailyRewardMail _:
                    return L10nManager.Localize("UI_DAILY_REWARD_MAIL_FORMAT");
                case MonsterCollectionMail _:
                    return L10nManager.Localize("UI_MONSTER_COLLECTION_MAIL_FORMAT");
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

            return (ItemBase) attachment.tradableFungibleItem;
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
                    return string.Format(L10nManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT"),
                        L10nManager.LocalizeItemName(collectQuest.ItemId));
                case CombinationQuest combinationQuest:
                    return string.Format(
                        L10nManager.Localize("QUEST_COMBINATION_CURRENT_INFO_FORMAT"),
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
                            return string.Format(
                                L10nManager.Localize($"QUEST_GENERAL_{generalQuest.Event}_FORMAT"),
                                generalQuest.Goal);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    return L10nManager.Localize($"QUEST_GENERAL_{generalQuest.Event}_FORMAT");
                case GoldQuest goldQuest:
                    return string.Format(
                        L10nManager.Localize($"QUEST_GOLD_{goldQuest.Type}_FORMAT"),
                        goldQuest.Goal);
                case ItemEnhancementQuest itemEnhancementQuest:
                    return string.Format(L10nManager.Localize("QUEST_ITEM_ENHANCEMENT_FORMAT"),
                        itemEnhancementQuest.Grade, itemEnhancementQuest.Goal,
                        itemEnhancementQuest.Count);
                case ItemGradeQuest itemGradeQuest:
                    return string.Format(L10nManager.Localize("QUEST_ITEM_GRADE_FORMAT"),
                        itemGradeQuest.Grade, itemGradeQuest.Goal);
                case ItemTypeCollectQuest itemTypeCollectQuest:
                    return string.Format(L10nManager.Localize("QUEST_ITEM_TYPE_FORMAT"),
                        itemTypeCollectQuest.ItemType.GetLocalizedString(),
                        itemTypeCollectQuest.Goal);
                case MonsterQuest monsterQuest:
                    return string.Format(L10nManager.Localize("QUEST_MONSTER_FORMAT"),
                        L10nManager.LocalizeCharacterName(monsterQuest.MonsterId));
                case TradeQuest tradeQuest:
                    return string.Format(L10nManager.Localize("QUEST_TRADE_CURRENT_INFO_FORMAT"),
                        tradeQuest.Type.GetLocalizedString(), tradeQuest.Goal);
                case WorldQuest worldQuest:
                    if (!Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(worldQuest.Goal,
                        out var worldRow))
                    {
                        worldRow = Game.Game.instance.TableSheets.WorldSheet.Last;
                        if (worldRow is null)
                        {
                            return string.Empty;
                        }
                    }

                    if (worldQuest.Goal == worldRow.StageBegin)
                    {
                        var format = L10nManager.Localize("QUEST_WORLD_FORMAT");
                        return string.Format(format, worldRow.GetLocalizedName());
                    }
                    else
                    {
                        var format = L10nManager.Localize("QUEST_CLEAR_STAGE_FORMAT");
                        return string.Format(format, worldRow.GetLocalizedName(), worldQuest.Goal);
                    }
                case CombinationEquipmentQuest combinationEquipmentQuest:
                    var unlockFormat = L10nManager.Localize("QUEST_COMBINATION_EQUIPMENT_FORMAT");
                    var itemId = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values
                        .First(r => r.Id == combinationEquipmentQuest.RecipeId).ResultEquipmentId;
                    return string.Format(unlockFormat, L10nManager.LocalizeItemName(itemId));
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

        public static string GetLocalizedNonColoredName(this ItemBase item, bool useElementalIcon = true)
        {
            var elemental = useElementalIcon ? GetElementalIcon(item.ElementalType) : string.Empty;
            var name = L10nManager.Localize($"ITEM_NAME_{item.Id}");
            return $"{name}{elemental}";
        }

        public static Color GetElementalTypeColor(this ItemBase item)
        {
            switch (item.ElementalType)
            {
                case ElementalType.Normal:
                    return Palette.GetColor(EnumType.ColorType.TextElement00);
                case ElementalType.Fire:
                    return Palette.GetColor(EnumType.ColorType.TextElement01);
                case ElementalType.Land:
                    return Palette.GetColor(EnumType.ColorType.TextElement02);
                case ElementalType.Water:
                    return Palette.GetColor(EnumType.ColorType.TextElement04);
                case ElementalType.Wind:
                    return Palette.GetColor(EnumType.ColorType.TextElement05);
                default:
                    return Color.white;
            }
        }
        public static Color GetItemGradeColor(this ItemBase item)
        {
            switch (item.Grade)
            {
                case 1:
                    return Palette.GetColor(EnumType.ColorType.TextGrade00);
                case 2:
                    return Palette.GetColor(EnumType.ColorType.TextGrade01);
                case 3:
                    return Palette.GetColor(EnumType.ColorType.TextGrade02);
                case 4:
                    return Palette.GetColor(EnumType.ColorType.TextGrade03);
                case 5:
                    return Palette.GetColor(EnumType.ColorType.TextGrade04);
                default:
                    return Palette.GetColor(EnumType.ColorType.TextGrade00);
            }
        }
        public static string GetColorHexByGrade(this ItemBase item)
        {
            var color = GetItemGradeColor(item);
            return color.ColorToHex();
        }

        public static string ColorToHex(this Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);

            var result = string.Format("{0:x2}{1:x2}{2:x2}", r, g, b);
            return result;
        }
        public static string GetLocalizedDescription(this ItemBase item)
        {
            return L10nManager.Localize($"ITEM_DESCRIPTION_{item.Id}");
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
            var gradeText = L10nManager.Localize($"UI_ITEM_GRADE_{itemBase.Grade}");
            return gradeText;
        }
        public static string GetSubTypeText(this ItemBase itemBase)
        {
            var subTypeText = GetLocalizedItemSubTypeText(itemBase.ItemSubType);
            return subTypeText;
        }
    }
}
