using System;
using System.Globalization;
using Nekoyume.EnumType;
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
            valueText.text = statType == StatType.SPD
                ? $"{(valueRange.valueMin / 100f).ToString(CultureInfo.InvariantCulture)} - {(valueRange.valueMax / 100f).ToString(CultureInfo.InvariantCulture)}"
                : $"{valueRange.valueMin} - {valueRange.valueMax}";
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
                additionalText.text = (additionalStatValue > 0)
                    ? $"({GetStatString(statType, additionalStatValue, true)})"
                    : $"<color=red>({GetStatString(statType, additionalStatValue, true)})</color>";
            }

            gameObject.SetActive(true);
        }

        protected string GetStatString(StatType statType, int value, bool isSigned = false)
        {
            string str = string.Empty;

            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                    str = isSigned
                        ? value.ToString("+0.#;-0.#")
                        : value.ToString();
                    break;
                case StatType.SPD:
                    str = isSigned
                        ? (value / 100f).ToString("+0.#;-0.#", CultureInfo.InvariantCulture)
                        : (value / 100f).ToString(CultureInfo.InvariantCulture);
                    break;
                case StatType.CRI:
                case StatType.DOG:
                    str = isSigned
                        ? value.ToString("+0.#\\%;-0.#\\%")
                        : $"{value:0.#\\%}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }

            return str;
        }
    }
}
