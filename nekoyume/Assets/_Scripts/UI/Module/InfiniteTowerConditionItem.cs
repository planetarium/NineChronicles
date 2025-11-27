using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Stat;
using Nekoyume.Model.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class InfiniteTowerConditionItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI guaranteedText;
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private TextMeshProUGUI conditionText;  // 조건 설명 텍스트

        public void SetCondition(InfiniteTowerCondition condition, bool isGuaranteed = false, float? weightPercentage = null)
        {
            SetBuffCondition(condition, isGuaranteed, weightPercentage);
        }

        private void SetBuffCondition(InfiniteTowerCondition condition, bool isGuaranteed, float? weightPercentage)
        {
            // Guaranteed 조건이거나 weight가 없는 경우 기존과 동일하게 표시
            if (isGuaranteed || !weightPercentage.HasValue)
            {
                guaranteedText.text = isGuaranteed
                    ? L10nManager.Localize("UI_CONDITION_REQUIRED")
                    : L10nManager.Localize("UI_CONDITION_RANDOM");
            }
            else
            {
                // 랜덤 조건이고 weight가 있는 경우 비율 표시
                var randomText = L10nManager.Localize("UI_CONDITION_RANDOM");
                guaranteedText.text = $"{randomText} ({weightPercentage.Value:F2}%)";
            }

            targetText.text = condition.TargetType == null || !condition.TargetType.Any()
                ? string.Empty
                : string.Join(", ", condition.TargetType.Select(t => t.GetLocalizedString()));
            var statModifier = condition.GetStatModifier();
            conditionText.text = statModifier.StatModifierToString();
        }

        public void SetBattleCondition(InfiniteTowerBattleCondition condition)
        {
            targetText.text = string.Empty;
            guaranteedText.text = condition.Type.GetLocalizedString();
            conditionText.text = condition.GetConditionText();
        }
    }
}
