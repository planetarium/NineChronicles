using System.Globalization;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StatView : MonoBehaviour
    {
        public TextMeshProUGUI statTypeText;
        public TextMeshProUGUI valueText;

        public virtual void Show(DecimalStat decimalStat)
        {
            Show(decimalStat.StatType, decimalStat.TotalValueAsLong);
        }

        public virtual void Show(StatType statType, long value, bool showPlus = false)
        {
            var valueString = statType.ValueToString(value);
            Show(statType.ToString(), showPlus ? $"+{valueString}" : valueString);
        }

        public virtual void Show(string statType, string value)
        {
            statTypeText.text = statType;
            valueText.text = value;
            Show();
        }

        public virtual void Show()
        {
            statTypeText.enabled = true;
            valueText.enabled = true;
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            statTypeText.enabled = false;
            valueText.enabled = false;
        }
    }
}
