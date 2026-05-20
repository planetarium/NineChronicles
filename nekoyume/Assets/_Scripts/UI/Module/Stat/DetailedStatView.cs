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
                NcDebug.LogError("Failed to parse StatType.");
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

        public void ShowModify(StatType statType, long addValue, long percentageValue)
        {
            const string none = "-";
            statTypeText.text = statType.ToString();
            valueText.text = addValue > 0 ? $"+{statType.ValueToString(addValue)}" : none;
            valueText2.text = percentageValue > 0 ? $"+{percentageValue:0.#\\%}" : none;

            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
