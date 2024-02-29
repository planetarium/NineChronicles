using Nekoyume.Helper;
using Nekoyume.UI.Module.WorldBoss;
using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossResultPopup : PopupWidget
    {
        [SerializeField]
        private Transform gradeParent;

        [SerializeField]
        private GameObject seasonBestObject;

        [SerializeField]
        private TextMeshProUGUI scoreText;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private List<RuneStoneItem> rewardViews;

        [SerializeField]
        private GameObject _practiceText;

        [SerializeField]
        private GameObject[] seasonPassObjs;

        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        private GameObject _gradeObject;

        private List<FungibleAssetValue> _killRewards;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
        }

        protected override void OnDisable()
        {
            Destroy(_gradeObject);
            base.OnDisable();
        }

        public void Show(
            int bossId,
            long score,
            bool isBest,
            List<FungibleAssetValue> battleRewards,
            List<FungibleAssetValue> killRewards)
        {
            base.Show();
            AudioController.instance.PlayMusic(AudioController.MusicCode.WorldBossBattleResult);
            _practiceText.SetActive(false);
            scoreText.text = score.ToString("N0");
            seasonBestObject.SetActive(isBest);

            if (Game.Game.instance.TableSheets.WorldBossCharacterSheet.TryGetValue(bossId, out var row))
            {
                var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(row, score);

                if (WorldBossFrontHelper.TryGetGrade(grade, false, out var prefab))
                {
                    _gradeObject = Instantiate(prefab, gradeParent);
                    _gradeObject.transform.localScale = Vector3.one;
                }
            }

            foreach (var view in rewardViews)
            {
                view.gameObject.SetActive(false);
            }

            _killRewards = killRewards;
            if (battleRewards is not null && battleRewards.Any())
            {
                foreach (var reward in battleRewards)
                {
                    var ticker = reward.Currency.Ticker;
                    if (RuneFrontHelper.TryGetRuneData(ticker, out var data))
                    {
                        var view = rewardViews.First(x => !x.gameObject.activeSelf);
                        var count = MathematicsExtensions.ConvertToInt32(reward.GetQuantityString());
                        view.Set(data, count);
                        view.gameObject.SetActive(true);
                    }
                }
            }

            RefreshSeasonPassCourageAmount();
        }

        public void ShowAsPractice(int bossId, long score)
        {
            foreach (var view in rewardViews)
            {
                view.gameObject.SetActive(false);
            }

            base.Show();
            AudioController.instance.PlayMusic(AudioController.MusicCode.WorldBossBattleResult);
            _practiceText.SetActive(true);
            scoreText.text = score.ToString("N0");
            seasonBestObject.SetActive(false);

            foreach (var view in rewardViews)
            {
                view.gameObject.SetActive(false);
            }

            if (Game.Game.instance.TableSheets.WorldBossCharacterSheet.TryGetValue(bossId, out var row))
            {
                var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(row, score);

                if (WorldBossFrontHelper.TryGetGrade(grade, false, out var prefab))
                {
                    _gradeObject = Instantiate(prefab, gradeParent);
                    _gradeObject.transform.localScale = Vector3.one;
                }
            }

            foreach (var item in seasonPassObjs)
            {
                item.SetActive(false);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (_killRewards is not null && _killRewards.Any())
            {
                Find<WorldBossRewardScreen>().Show(_killRewards,
                    () =>
                    {
                        Find<WorldBoss>().ShowAsync().Forget();
                    });
            }
            else
            {
                Find<WorldBoss>().ShowAsync().Forget();
            }

            base.Close(ignoreCloseAnimation);
        }

        private void RefreshSeasonPassCourageAmount()
        {
            if (Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }
                seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.WorldBossCourageAmount}";
            }
            else
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(false);
                }
            }
        }
    }
}
