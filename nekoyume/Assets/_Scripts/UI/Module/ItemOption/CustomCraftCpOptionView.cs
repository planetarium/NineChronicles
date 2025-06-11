using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CustomCraftCpOptionView : CoveredItemOptionView
    {
        [SerializeField]
        private TextMeshProUGUI statTypeText;

        public void UpdateView(long cp, DecimalStat stat)
        {
            UpdateView(TextHelper.FormatNumber(cp), $"+{TextHelper.FormatNumber(stat.AdditionalValueAsLong)}");
            statTypeText.SetText(stat.StatType.ToString());
        }
    }
}
