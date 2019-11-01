using System;
using Nekoyume.EnumType;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class StatusInfo : MonoBehaviour
    {
        public Text key;
        public Text value;
        public Text additional;

        public void Set(StatType statType, int statValue, int additionalStatValue)
        {
            key.text = statType.ToString();
            
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.SPD:
                    value.text = statValue.ToString();
                    additional.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue})";
                    break;
                case StatType.CRI:
                case StatType.DOG:
                    value.text = $"{statValue:0.#\\%}";
                    additional.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue:0.#\\%})";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
            
            gameObject.SetActive(true);
        }

        public void SetAdditional(StatType statType, int additionalStatValue)
        {
            if (additionalStatValue == 0) additional.text = string.Empty;

            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.SPD:
                    additional.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue})";
                    break;
                case StatType.CRI:
                case StatType.DOG:
                    additional.text = Mathf.Approximately(additionalStatValue, 0f)
                        ? ""
                        : $"(+{additionalStatValue:0.#\\%})";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }

            gameObject.SetActive(true);
        }
    }
}
