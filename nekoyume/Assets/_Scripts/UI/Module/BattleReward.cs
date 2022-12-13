using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class BattleReward : MonoBehaviour
    {
        public int index;
        public StarArea starArea;
        public RewardItems rewardItems;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI failedText;
        public StarForMulti starForMulti;
        public Animator animator;

        private Star _star;
        private string _stageClearText;
        private Tweener _tweener = null;

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
            public VFX emissionVFX;
            public Star01VFX starVFX;

            public IEnumerator Set(bool enable)
            {
                if (enable)
                {
                    emissionVFX.Play();
                    starVFX.Play();
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    StopVFX();
                }

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

            public void StopVFX()
            {
                emissionVFX.Stop();
                starVFX.Stop();
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
                    var rt = items[i].RectTransform;
                    var itemBase = rewardItems[i].ItemBase.Value;
                    items[i].SetData(rewardItems[i], () => ShowTooltip(itemBase));
                    items[i].gameObject.SetActive(true);
                }
            }

            private static void ShowTooltip(ItemBase itemBase)
            {
                AudioController.PlayClick();
                var tooltip = ItemTooltip.Find(itemBase.ItemType);
                tooltip.Show(itemBase, string.Empty, false, null);
            }
        }

        [Serializable]
        public struct StarForMulti
        {
            public TextMeshProUGUI StarCountText;
            public TextMeshProUGUI[] WaveStarTexts;
            public TextMeshProUGUI RemainingStarText;
        }

        private void Awake()
        {
            if (index > -1)
            {
                rewardItems.gameObject.SetActive(false);
                failedText.text = GetFailedText();
                _stageClearText = L10nManager.Localize("UI_BATTLE_RESULT_CLEAR");
                _star = starArea.stars[index];
                StartCoroutine(_star.Set(false));
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
                        StartCoroutine(star.Set(false));
                    }
                }
            }

            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                _stageClearText = L10nManager.Localize("UI_BATTLE_RESULT_CLEAR");
                if (index > -1)
                {
                    failedText.text = GetFailedText();
                }
            });
        }

        private void OnDisable()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        public void Set(long exp, bool enable)
        {
            rewardText.text = $"EXP + {exp}";
            failedText.gameObject.SetActive(!enable);
            rewardText.gameObject.SetActive(enable);
        }

        public void Set(IReadOnlyList<CountableItem> items, int stageId, bool cleared)
        {
            rewardItems.gameObject.SetActive(cleared);
            rewardItems.Set(items);
            rewardText.gameObject.SetActive((stageId == 1 || !items.Any()) && cleared);
            rewardText.text = stageId == 1
                ? L10nManager.Localize("UI_BATTLE_RESULT_STAGE_1")
                : GetFailedText();

            failedText.gameObject.SetActive(!cleared);
            failedText.text = GetFailedText();
        }

        public void Set(bool cleared)
        {
            if (cleared)
            {
                rewardText.text = _stageClearText;
            }

            rewardText.gameObject.SetActive(cleared);
            failedText.gameObject.SetActive(!cleared);
        }

        public void Set(int[] clearedWaves, int starCount, int maxStarCount)
        {
            starForMulti.StarCountText.text = $"{starCount}/{maxStarCount}";
            for (int i = 0; i < starForMulti.WaveStarTexts.Length; i++)
            {
                starForMulti.WaveStarTexts[i].text = clearedWaves[i].ToString();
            }

            starForMulti.RemainingStarText.text =
                starCount >= maxStarCount
                    ? L10nManager.Localize("UI_ENOUGH_STAR_TO_BUFF")
                    : L10nManager.Localize("UI_REMAINING_STAR_TO_BUFF", maxStarCount - starCount);
        }

        private string GetFailedText()
        {
            switch (index)
            {
                case 0:
                    return L10nManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_0");
                case 1:
                    return L10nManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_1");
                case 2:
                    return L10nManager.Localize("UI_BATTLE_RESULT_FAILED_PHASE_2");
                default:
                    return string.Empty;
            }
        }

        public void EnableStar(bool enable)
        {
            StartCoroutine(_star.Set(enable));
        }

        public void StopVFX()
        {
            _star.StopVFX();
        }

        public void StartShowAnimation()
        {
            animator.enabled = true;
        }

        public void StopShowAnimation()
        {
            animator.enabled = false;
        }

        public void StartScaleTween()
        {
            _tweener?.Kill();
            _tweener = transform
                .DOScale(1.05f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
}
