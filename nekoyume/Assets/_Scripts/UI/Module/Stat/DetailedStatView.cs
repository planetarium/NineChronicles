using System;
using System.Globalization;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedStatView : StatView
    {
        public TextMeshProUGUI additionalText;

        public void Show(StatType statType, int statValue, int additionalStatValue)
        {
            statTypeText.text = statType.ToString();
            valueText.text = GetStatString(statType, statValue);
            SetAdditional(statType, additionalStatValue);
        }

        public void Show(StatType statType, (int valueMin, int valueMax) valueRange)
        {
            statTypeText.text = statType.ToString();
            var valueMin = statType.ValueToString(valueRange.valueMin);
            var valueMax = statType.ValueToString(valueRange.valueMax);

            valueText.text = $"{valueMin} - {valueMax}";
            additionalText.text = string.Empty;
            gameObject.SetActive(true);
        }

        public void Show(string keyText, int statValue, int additionalStatValue)
        {
            if (!Enum.TryParse<StatType>(keyText, out var statType))
            {
                Debug.LogError("Failed to parse StatType.");
            }

            Show(statType, statValue, additionalStatValue);
        }

        public void SetAdditional(StatType statType, int additionalStatValue)
        {
            if (additionalStatValue == 0)
            {
                additionalText.text = string.Empty;
            }
            else
            {
                additionalText.text = additionalStatValue > 0
                    ? $"({GetStatString(statType, additionalStatValue, true)})"
                    : $"<color=red>({GetStatString(statType, additionalStatValue, true)})</color>";
            }

            gameObject.SetActive(true);
        }

        protected string GetStatString(StatType statType, int value, bool isSigned = false)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.DRV:
                    return isSigned
                        ? value.ToString("+0.##;-0.##")
                        : value.ToString();
                case StatType.CRI:
                    return isSigned
                        ? value.ToString("+0.##\\%;-0.##\\%")
                        : $"{value:0.#\\%}";
                case StatType.SPD:
                case StatType.DRR:
                case StatType.CDMG:
                    return isSigned
                        ? (value / 100f).ToString("+0.##;-0.##", CultureInfo.InvariantCulture)
                        : (value / 100f).ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }
    }
}
