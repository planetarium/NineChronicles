using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
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
                    var buyerItemName = Util.GetItemNameByOrdierId(orderBuyerMail.OrderId, true);
                    return string.Format(L10nManager.Localize("UI_BUYER_MAIL_FORMAT"),
                        buyerItemName);

                case OrderSellerMail orderSellerMail:
                    var order = Util.GetOrder(orderSellerMail.OrderId);
                    var sellerItemName = Util.GetItemNameByOrdierId(orderSellerMail.OrderId, true);
                    var format = L10nManager.Localize("UI_SELLER_MAIL_FORMAT");
                    var taxedPrice = order.Price - order.GetTax();
                    return string.Format(format, taxedPrice, sellerItemName);

                case OrderExpirationMail orderExpirationMail:
                    var expiredItemName = Util.GetItemNameByOrdierId(orderExpirationMail.OrderId, true);
                    return string.Format(L10nManager.Localize("UI_SELL_EXPIRATION_MAIL_FORMAT"),
                        expiredItemName);

                case CancelOrderMail cancelOrderMail:
                    var cancelItemName = Util.GetItemNameByOrdierId(cancelOrderMail.OrderId, true);
                    return string.Format(L10nManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT"),
                        cancelItemName);

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

        public static string GetLocalizedName(this ItemBase item)
        {
            string name = item.GetLocalizedNonColoredName();
            switch (item)
            {
                case Equipment equipment:
                    return equipment.level > 0
                        ? $"<color=#{GetColorHexByGrade(item)}>+{equipment.level} {name}</color>"
                        : $"<color=#{GetColorHexByGrade(item)}>{name}</color>";
                default:
                    return $"<color=#{GetColorHexByGrade(item)}>{name}</color>";
            }
        }

        public static string GetLocalizedName(EquipmentItemSheet sheet, int equipmentId, int level)
        {
            var grade = sheet[equipmentId].Grade;
            var name = GetLocalizedNonColoredName(equipmentId);

            return level > 0
                ? $"<color=#{GetColorHexByGrade(grade)}>+{level} {name}</color>"
                : $"<color=#{GetColorHexByGrade(grade)}>{name}</color>";
        }

        public static string GetLocalizedNonColoredName(this ItemBase item)
        {
            return GetLocalizedNonColoredName(item.Id);
        }

        public static string GetLocalizedNonColoredName(int id)
        {
            return L10nManager.Localize($"ITEM_NAME_{id}");
        }

        public static Color GetItemGradeColor(this ItemBase item)
        {
            return ColorHelper.HexToColorRGB(GetColorHexByGrade(item));
        }

        public static string GetLocalizedDescription(this ItemBase item)
        {
            return L10nManager.Localize($"ITEM_DESCRIPTION_{item.Id}");
        }

        private static string GetColorHexByGrade(ItemBase item)
        {
            return GetColorHexByGrade(item.Grade);
        }

        private static string GetColorHexByGrade(int grade)
        {
            switch (grade)
            {
                case 1:
                    return ColorConfig.ColorHexForGrade1;
                case 2:
                    return ColorConfig.ColorHexForGrade2;
                case 3:
                    return ColorConfig.ColorHexForGrade3;
                case 4:
                    return ColorConfig.ColorHexForGrade4;
                case 5:
                    return ColorConfig.ColorHexForGrade5;
                default:
                    return ColorConfig.ColorHexForGrade1;
            }
        }
    }
}
