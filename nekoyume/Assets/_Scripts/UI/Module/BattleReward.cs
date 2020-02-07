using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BattleReward : MonoBehaviour
    {
        public int index;
        public StarArea starArea;
        public RewardItems rewardItems;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI failedText;

        private Star _star;
        private string _stageClearText;

        [Serializable]
        public struct StarArea
        {
            public Star[] stars;
        }

        [Serializable]
        public struct Star
        {
            public Image emptyStar;
            public Image enabledStar;

            public void Set(bool enable)
            {
                emptyStar.gameObject.SetActive(!enable);
                enabledStar.gameObject.SetActive(enable);
                emptyStar.SetNativeSize();
                enabledStar.SetNativeSize();
            }

            public void Disable()
            {
                emptyStar.gameObject.SetActive(false);
                enabledStar.gameObject.SetActive(false);
            }
        }

        [Serializable]
        public struct RewardItems
        {
            public GameObject gameObject;
            public SimpleCountableItemView[] items;

            public void Set(IReadOnlyList<CountableItem> rewardItems)
            {
                foreach (var view in items)
                {
                    view.gameObject.SetActive(false);
                }
                for (var i = 0; i < rewardItems.Count; i++)
                {
                    items[i].SetData(rewardItems[i]);
                    items[i].gameObject.SetActive(true);
                }
            }
        }

        private void Awake()
        {
            rewardItems.gameObject.SetActive(false);
            failedText.text = GetFailedText();
            _stageClearText = LocalizationManager.Localize("UI_BATTLE_RESULT_CLEAR");
            _star = starArea.stars[index];
            _star.Set(false);
            failedText.gameObject.SetActive(true);
            for (var i = 0; i < starArea.stars.Length; i++)
            {
                var star = starArea.stars[i];
                if (i != index)
                {
                    star.Disable();
                }
                else
                {
                    star.Set(false);
                }
            }
        }

        public void Set(long exp, bool enable)
        {
            rewardText.text = $"EXP + {exp}";
            failedText.gameObject.SetActive(false);
            rewardText.gameObject.SetActive(true);
            _star.Set(enable);
        }

        public void Set(IReadOnlyList<CountableItem> items, bool enable)
        {
            rewardItems.gameObject.SetActive(true);
            rewardItems.Set(items);
            rewardText.gameObject.SetActive(false);
            failedText.gameObject.SetActive(!items.Any());
            _star.Set(enable);
        }

        public void Set(bool cleared)
        {
            if (cleared)
            {
                rewardText.text = _stageClearText;
                failedText.gameObject.SetActive(false);
            }
            else
            {
                rewardText.gameObject.SetActive(false);
                failedText.gameObject.SetActive(true);
            }
            _star.Set(cleared);
        }

        private string GetFailedText()
        {
            switch (index)
            {
                case 0:
                    return LocalizationManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_0");
                case 1:
                    return LocalizationManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_1");
                case 2:
                    return LocalizationManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_2");
                default:
                    return string.Empty;
            }
        }
    }
}
