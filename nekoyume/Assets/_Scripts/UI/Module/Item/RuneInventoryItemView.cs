using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneInventoryItemView : MonoBehaviour
    {
        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private List<Image> runeIcons;

        [SerializeField]
        private List<TextMeshProUGUI> runeCounts;

        [SerializeField]
        private TextMeshProUGUI bossName;

        public void Set(RuneInventoryItem model, RuneInventoryScroll.ContextModel context)
        {
            if (WorldBossFrontHelper.TryGetBossData(model.BoosId, out var data))
            {
                bossName.text = data.name;
                bossImage.sprite = data.illustration;
            }

            for (var i = 0; i < model.Runes.Count; i++)
            {
                var ticker = model.Runes[i].Currency.Ticker;
                if (WorldBossFrontHelper.TryGetRuneIcon(ticker, out var icon))
                {
                    runeIcons[i].sprite = icon;
                    var count = Convert.ToInt32(model.Runes[i].GetQuantityString());
                    runeCounts[i].text = $"{count:#,0}";
                }
            }
        }
    }
}
