using Nekoyume.EnumType;
using Nekoyume.Game;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StatView : MonoBehaviour
    {
        public TextMeshProUGUI statTypeText;
        public TextMeshProUGUI valueText;

        public void Show(IntStat intStat)
        {
            Show(intStat.Type, intStat.Value);
        }
        
        public void Show(DecimalStat decimalStat)
        {
            Show(decimalStat.Type, (int) decimalStat.Value);
        }

        public void Show(StatMap statMap)
        {
            Show(statMap.StatType, (int) statMap.Value);
        }
        
        public void Show(StatMapEx statMapEx)
        {
            Show(statMapEx.StatType, statMapEx.TotalValueAsInt);
        }

        public void Show(StatType statType, int value)
        {
            Show(statType.ToString(), value.ToString());
        }

        public void Show(string statType, string value)
        {
            statTypeText.text = statType;
            valueText.text = value;
            Show();
        }

        public virtual void Show()
        {
            statTypeText.enabled = true;
            valueText.enabled = true;
        }

        public virtual void Hide()
        {
            statTypeText.enabled = false;
            valueText.enabled = false;
        }
    }
}
