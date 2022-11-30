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
    public class RuneStoneInventoryItemView : MonoBehaviour
    {
        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private TextMeshProUGUI bossName;

        [SerializeField]
        private List<RuneStoneItem> runeStones;

        public void Set(RuneStoneInventoryItem model, RuneStoneInventoryScroll.ContextModel context)
        {
            if (WorldBossFrontHelper.TryGetBossData(model.BoosId, out var data))
            {
                bossName.text = data.name;
                bossImage.sprite = data.illustration;
            }

            for (var i = 0; i < model.Runes.Count; i++)
            {
                var ticker = model.Runes[i].Currency.Ticker;
                if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
                {
                    var count = Convert.ToInt32(model.Runes[i].GetQuantityString());
                    runeStones[i].Set(runeData, count);
                }
            }
        }
    }
}
