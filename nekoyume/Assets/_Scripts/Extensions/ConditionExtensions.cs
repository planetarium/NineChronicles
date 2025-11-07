using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.InfiniteTower;
using static Nekoyume.LocalizationExtensions;

namespace Nekoyume
{
    public static class ConditionExtensions
    {
        public static string GetConditionText(this InfiniteTowerBattleCondition condition)
        {
            // BattleConditionType에 따라 텍스트 생성
            // 실제 BattleConditionType enum이 어떻게 정의되어 있는지 확인 필요
            // 임시로 조건의 속성들을 직접 확인하여 텍스트 생성
            var parts = new List<string>();
            switch (condition.Type)
            {
                case BattleConditionType.CP:
                {
                    if (condition.RequiredCp.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MIN_CP", condition.RequiredCp.Value.ToString("#,0")));
                    }

                    if (condition.MaxCp.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MAX_CP", condition.MaxCp.Value.ToString("#,0")));
                    }

                    return parts.Count > 0 ? $"{string.Join("-", parts)}" : string.Empty;
                }
                case BattleConditionType.ItemGrade:
                    if (condition.MinItemGrade.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MIN_ITEM_GRADE", condition.MinItemGrade.Value));
                    }

                    if (condition.MaxItemGrade.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MAX_ITEM_GRADE", condition.MaxItemGrade.Value));
                    }

                    return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
                case BattleConditionType.ItemLevel:
                    if (condition.MinItemLevel.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MIN_ITEM_LEVEL", condition.MinItemLevel.Value));
                    }

                    if (condition.MaxItemLevel.HasValue)
                    {
                        parts.Add(L10nManager.Localize("UI_CONDITION_MAX_ITEM_LEVEL", condition.MaxItemLevel.Value));
                    }

                    return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
                case BattleConditionType.ForbiddenRuneTypes:
                    var runeTypeNames = condition.ForbiddenRuneTypes.Select(r => r.GetLocalizedString()).ToList();
                    return L10nManager.Localize("UI_CONDITION_FORBIDDEN_RUNE_TYPES", string.Join(", ", runeTypeNames));
                case BattleConditionType.RequiredElementalType:
                    var elementalIcons = string.Join("", condition.RequiredElementalTypes.Select(GetElementalIcon));
                    return L10nManager.Localize("UI_CONDITION_REQUIRED_ELEMENTAL_TYPE", elementalIcons);
                case BattleConditionType.ForbiddenItemSubTypes:
                    var itemTypeNames = condition.ForbiddenItemSubTypes.Select(GetLocalizedItemSubTypeText).ToList();
                    return L10nManager.Localize("UI_CONDITION_FORBIDDEN_ITEM_SUB_TYPES", string.Join(", ", itemTypeNames));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
