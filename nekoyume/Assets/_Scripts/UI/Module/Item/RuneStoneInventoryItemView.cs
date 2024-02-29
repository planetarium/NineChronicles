using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
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
                bossName.text = L10nManager.LocalizeCharacterName(data.id);
                bossImage.sprite = data.illustration;
            }

            for (var i = 0; i < model.Runes.Count; i++)
            {
                var ticker = model.Runes[i].Currency.Ticker;
                if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
                {
                    var count = MathematicsExtensions.ConvertToInt32(model.Runes[i].GetQuantityString());
                    runeStones[i].Set(runeData, count);
                }
            }
        }
    }
}
