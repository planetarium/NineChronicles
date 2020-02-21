using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
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
                case BuyerMail buyerMail:
                    return string.Format(
                        LocalizationManager.Localize("UI_BUYER_MAIL_FORMAT"), 
                        buyerMail.attachment.itemUsable.Data.GetLocalizedName()
                    );
                case CombinationMail combinationMail:
                    return string.Format(
                        LocalizationManager.Localize("UI_COMBINATION_NOTIFY_FORMAT"),
                        combinationMail.attachment.itemUsable.GetLocalizedName()
                    );
                case ItemEnhanceMail itemEnhanceMail:
                    return string.Format(
                        LocalizationManager.Localize("UI_ITEM_ENHANCEMENT_MAIL_FORMAT"),
                        itemEnhanceMail.attachment.itemUsable.Data.GetLocalizedName()
                    );
                case SellCancelMail sellCancelMail:
                    return string.Format(
                        LocalizationManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT"),
                        sellCancelMail.attachment.itemUsable.Data.GetLocalizedName()
                    );
                case SellerMail sellerMail:
                    var attachment = sellerMail.attachment;
                    if (!(attachment is Buy.SellerResult sellerResult))
                        throw new InvalidCastException($"({nameof(Buy.SellerResult)}){nameof(attachment)}");

                    var format = LocalizationManager.Localize("UI_SELLER_MAIL_FORMAT");
                    return string.Format(format, sellerResult.gold, sellerResult.itemUsable.Data.GetLocalizedName());
                default:
                    throw new NotSupportedException(
                        $"Given mail[{mail}] doesn't support {nameof(ToInfo)}() method."
                    );
            }
        }
        
        public static string GetTitle(this QuestModel quest)
        {
            switch (quest)
            {
                case CollectQuest collectQuest:
                    return LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_TITLE");
                case CombinationQuest combinationQuest:
                    return LocalizationManager.Localize("QUEST_COMBINATION_CURRENT_INFO_TITLE");
                case GeneralQuest generalQuest:
                    return LocalizationManager.Localize($"QUEST_GENERAL_{generalQuest.Event}_TITLE");
                case GoldQuest goldQuest:
                    return LocalizationManager.Localize($"QUEST_GOLD_{goldQuest.Type}_TITLE");
                case ItemEnhancementQuest itemEnhancementQuest:
                    return LocalizationManager.Localize("QUEST_ITEM_ENHANCEMENT_TITLE");
                case ItemGradeQuest itemGradeQuest:
                    return LocalizationManager.Localize("QUEST_ITEM_GRADE_TITLE");
                case ItemTypeCollectQuest itemTypeCollectQuest:
                    return LocalizationManager.Localize("QUEST_ITEM_TYPE_TITLE");
                case MonsterQuest monsterQuest:
                    return LocalizationManager.Localize("QUEST_MONSTER_TITLE");
                case TradeQuest tradeQuest:
                    return LocalizationManager.Localize("QUEST_TRADE_CURRENT_INFO_TITLE");
                case WorldQuest worldQuest:
                    if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(worldQuest.Goal, out var worldRow))
                    {
                        return LocalizationManager.Localize("QUEST_WORLD_TITLE");;
                    }
                    throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", worldQuest.Goal.ToString());
                default:
                    throw new NotSupportedException(
                        $"Given quest[{quest}] doesn't support {nameof(GetTitle)}() method."
                    );

            }
        }

        public static string GetContent(this QuestModel quest)
        {
            switch (quest)
            {
                case CollectQuest collectQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT"),
                        LocalizationManager.LocalizeItemName(collectQuest.ItemId)
                    );
                case CombinationQuest combinationQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_COMBINATION_CURRENT_INFO_FORMAT"),
                        combinationQuest.ItemSubType.GetLocalizedString()
                    );
                case GeneralQuest generalQuest:
                    return LocalizationManager.Localize($"QUEST_GENERAL_{generalQuest.Event}_FORMAT");
                case GoldQuest goldQuest:
                    return string.Format(
                        LocalizationManager.Localize($"QUEST_GOLD_{goldQuest.Type}_FORMAT"), 
                        goldQuest.Goal
                    );
                case ItemEnhancementQuest itemEnhancementQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_ITEM_ENHANCEMENT_FORMAT"), 
                        itemEnhancementQuest.Grade,
                        itemEnhancementQuest.Goal
                    );
                case ItemGradeQuest itemGradeQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_ITEM_GRADE_FORMAT"), 
                        itemGradeQuest.Grade
                    );
                case ItemTypeCollectQuest itemTypeCollectQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_ITEM_TYPE_FORMAT"), 
                        itemTypeCollectQuest.ItemType.GetLocalizedString()
                    );
                case MonsterQuest monsterQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_MONSTER_FORMAT"), 
                        LocalizationManager.LocalizeCharacterName(monsterQuest.MonsterId)
                    );
                case TradeQuest tradeQuest:
                    return string.Format(
                        LocalizationManager.Localize("QUEST_TRADE_CURRENT_INFO_FORMAT"), 
                        tradeQuest.Type.GetLocalizedString()
                    );
                case WorldQuest worldQuest:
                    if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(worldQuest.Goal, out var worldRow))
                    {
                        var format = LocalizationManager.Localize("QUEST_WORLD_FORMAT");
                        return string.Format(format, worldRow.GetLocalizedName());
                    }
                    throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", worldQuest.Goal.ToString());
                default:
                    throw new NotSupportedException(
                        $"Given quest[{quest}] doesn't support {nameof(GetContent)}() method."
                    );

            }
        }

        public static string GetLocalizedString(this ItemType value)
        {
            return LocalizationManager.Localize($"ITEM_TYPE_{value}");
        }

        public static string GetLocalizedString(this ItemSubType value)
        {
            return LocalizationManager.Localize($"ITEM_SUB_TYPE_{value}");
        }

        public static string GetLocalizedString(this TradeType value)
        {
            return LocalizationManager.Localize($"TRADE_TYPE_{value}");
        }

        public static string GetLocalizedString(this StatType value)
        {
            return LocalizationManager.Localize($"STAT_TYPE_{value}");
        }

        public static string GetLocalizedString(this ElementalType value)
        {
            return LocalizationManager.Localize($"ELEMENTAL_TYPE_{value.ToString().ToUpper()}");
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
            string name = item.Data.GetLocalizedName();
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

        public static string GetLocalizedDescription(this ItemBase item)
        {
            return item.Data.GetLocalizedDescription();
        }

        private static string GetColorHexByGrade(ItemBase item)
        {
            switch (item.Data.Grade)
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
