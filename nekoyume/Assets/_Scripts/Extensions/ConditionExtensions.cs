using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
                        parts.Add($"최소 {condition.RequiredCp.Value:#,0}");
                    }

                    if (condition.MaxCp.HasValue)
                    {
                        parts.Add($"최대 {condition.MaxCp.Value:#,0}");
                    }

                    return parts.Count > 0 ? $"{string.Join("-", parts)}" : string.Empty;
                }
                case BattleConditionType.ItemGrade:
                    if (condition.MinItemGrade.HasValue)
                    {
                        parts.Add($"{condition.MinItemGrade.Value} 이상");
                    }

                    if (condition.MaxItemGrade.HasValue)
                    {
                        parts.Add($"{condition.MaxItemGrade.Value} 이하");
                    }

                    return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
                case BattleConditionType.ItemLevel:
                    if (condition.MinItemLevel.HasValue)
                    {
                        parts.Add($"레벨 {condition.MinItemLevel.Value} 이상");
                    }

                    if (condition.MaxItemLevel.HasValue)
                    {
                        parts.Add($"레벨 {condition.MaxItemLevel.Value} 이하");
                    }

                    return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
                case BattleConditionType.ForbiddenRuneTypes:
                    var runeTypeNames = condition.ForbiddenRuneTypes.Select(r => r.GetLocalizedString()).ToList();
                    return $"{string.Join(", ", runeTypeNames)} 착용 불가";
                case BattleConditionType.RequiredElementalType:
                    return $"{condition.RequiredElementalTypes.Select(GetElementalIcon)} 장비만 착용 가능";
                case BattleConditionType.ForbiddenItemSubTypes:
                    var itemTypeNames = condition.ForbiddenItemSubTypes.Select(GetLocalizedItemSubTypeText).ToList();
                    return $"{string.Join(", ", itemTypeNames)} 착용 불가";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
