using System;
using System.Globalization;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ComparisonStatView : StatView
    {
        public TextMeshProUGUI afterValueText;

        public void Show(StatType statType, long statValue, long afterStatValue)
        {
            afterValueText.text = statType.ValueToString(afterStatValue);
            Show(statType, statValue);
        }

        public void Show(string keyText, long statValue, long afterStatValue)
        {
            if (!Enum.TryParse<StatType>(keyText, out var statType))
            {
                NcDebug.LogError("Failed to parse StatType.");
                return;
            }

            Show(statType, statValue, afterStatValue);
        }
    }
}
