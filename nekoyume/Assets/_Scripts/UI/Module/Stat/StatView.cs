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

        public virtual void Show(IntStat intStat)
        {
            Show(intStat.Type, intStat.Value);
        }
        
        public virtual void Show(DecimalStat decimalStat)
        {
            Show(decimalStat.Type, (int) decimalStat.Value);
        }

        public virtual void Show(StatMap statMap)
        {
            Show(statMap.StatType, (int) statMap.Value);
        }
        
        public virtual void Show(StatMapEx statMapEx)
        {
            Show(statMapEx.StatType, statMapEx.TotalValueAsInt);
        }

        public virtual void Show(StatType statType, int value, bool showPlus = false)
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
