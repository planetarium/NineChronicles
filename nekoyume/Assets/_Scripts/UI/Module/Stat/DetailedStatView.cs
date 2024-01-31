using System;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedStatView : StatView
    {
        public TextMeshProUGUI additionalText;
        public TextMeshProUGUI valueText2;

        public void Show(StatType statType, long statValue, long additionalStatValue)
        {
            statTypeText.text = statType.ToString();
            valueText.text = statType.ValueToString(statValue);
            SetAdditional(statType, additionalStatValue);
        }

        public void Show(StatType statType, (long valueMin, long valueMax) valueRange)
        {
            statTypeText.text = statType.ToString();
            var valueMin = statType.ValueToString(valueRange.valueMin);
            var valueMax = statType.ValueToString(valueRange.valueMax);

            valueText.text = $"{valueMin} - {valueMax}";
            additionalText.text = string.Empty;
            gameObject.SetActive(true);
        }

        public void Show(string keyText, long statValue, long additionalStatValue)
        {
            if (!Enum.TryParse<StatType>(keyText, out var statType))
            {
                Debug.LogError("Failed to parse StatType.");
            }

            Show(statType, statValue, additionalStatValue);
        }

        public void SetAdditional(StatType statType, long additionalStatValue)
        {
            if (additionalStatValue == 0)
            {
                additionalText.text = string.Empty;
            }
            else
            {
                additionalText.text = additionalStatValue > 0
                    ? $"({statType.ValueToString(additionalStatValue, true)})"
                    : $"<color=red>({statType.ValueToString(additionalStatValue, true)})</color>";
            }

            gameObject.SetActive(true);
        }

        public void Show(
            StatType statType,
            StatModifier.OperationType operationType1, long value1,
            StatModifier.OperationType operationType2, long value2)
        {
            statTypeText.text = statType.ToString();
            valueText.text = operationType1 == StatModifier.OperationType.Add
                ? $"+{statType.ValueToString(value1)}"
                : $"+{value1}%";
            valueText2.text = operationType2 == StatModifier.OperationType.Add
                ? $"+{statType.ValueToString(value2)}"
                : $"+{value2}%";

            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
