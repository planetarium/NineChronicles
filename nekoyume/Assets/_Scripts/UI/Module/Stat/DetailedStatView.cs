using System;
using Nekoyume.EnumType;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedStatView : StatView
    {
        public TextMeshProUGUI additionalText;

        public void Set(StatType statType, int statValue, int additionalStatValue)
        {
            statTypeText.text = statType.ToString();

            valueText.text = GetStatString(statType, statValue);
            additionalText.text = $"+({GetStatString(statType, additionalStatValue)})";

            gameObject.SetActive(true);
        }

        public void Set(string keyText, int statValue, int additionalStatValue)
        {
            if(!Enum.TryParse<StatType>(keyText, out var statType))
            {
                Debug.LogError("Failed to parse StatType.");
            }

            Set(statType, statValue, additionalStatValue);
        }

        public void SetAdditional(StatType statType, int additionalStatValue)
        {
            if (additionalStatValue == 0) additionalText.text = string.Empty;

            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.SPD:
                    additionalText.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue})";
                    break;
                case StatType.CRI:
                case StatType.DOG:
                    additionalText.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue:0.#\\%})";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }

            gameObject.SetActive(true);
        }

        protected string GetStatString(StatType statType, int value)
        {
            switch(statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.SPD:
                    return value.ToString();
                case StatType.CRI:
                case StatType.DOG:
                    return $"{value:0.#\\%}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }
    }
}
