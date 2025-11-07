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

        public void SetCondition(InfiniteTowerCondition condition, bool isGuaranteed = false)
        {
            SetBuffCondition(condition, isGuaranteed);
        }

        private void SetBuffCondition(InfiniteTowerCondition condition, bool isGuaranteed)
        {
            guaranteedText.text = isGuaranteed ? "Required" : "Random";
            targetText.text = condition.TargetType == null || !condition.TargetType.Any()
                ? string.Empty
                : string.Join(", ", condition.TargetType);
            var statModifier = condition.GetStatModifier();
            conditionText.text = statModifier.StatModifierToString();
        }

        public void SetBattleCondition(InfiniteTowerBattleCondition condition)
        {
            targetText.text = string.Empty;
            guaranteedText.text = condition.Type switch
            {
                BattleConditionType.CP => "CP",
                BattleConditionType.ItemGrade => "ItemGrade",
                BattleConditionType.ItemLevel => "ItemLevel",
                BattleConditionType.ForbiddenRuneTypes => "ForbiddenRuneTypes",
                BattleConditionType.RequiredElementalType => "RequiredElementalType",
                BattleConditionType.ForbiddenItemSubTypes => "ForbiddenItemSubTypes",
                _ => throw new ArgumentOutOfRangeException()
            };
            conditionText.text = condition.GetConditionText();
        }
    }
}
