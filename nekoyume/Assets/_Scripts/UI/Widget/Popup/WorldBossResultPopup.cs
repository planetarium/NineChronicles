using Nekoyume.Helper;
using Nekoyume.UI.Module.WorldBoss;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [FormerlySerializedAs("rewardViews")]
        [SerializeField]
        private List<RuneStoneItem> runeRewardViews;

        [SerializeField]
        private List<SimpleCountableItemView> itemRewardViews;

        [SerializeField]
        private GameObject _practiceText;

        [SerializeField]
        private GameObject[] seasonPassObjs;

        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        private GameObject _gradeObject;

        private WorldBossRewards _killRewards;

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
            WorldBossRewards battleRewards,
            WorldBossRewards killRewards)
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

            foreach (var view in runeRewardViews)
            {
                view.gameObject.SetActive(false);
            }

            foreach (var view in itemRewardViews)
            {
                view.Hide();
            }

            _killRewards = killRewards;
            if (battleRewards.Assets is not null && battleRewards.Assets.Any())
            {
                foreach (var reward in battleRewards.Assets)
                {
                    var ticker = reward.Currency.Ticker;
                    if (RuneFrontHelper.TryGetRuneData(ticker, out var data))
                    {
                        var view = runeRewardViews.First(x => !x.gameObject.activeSelf);
                        var count = MathematicsExtensions.ConvertToInt32(reward.GetQuantityString());
                        view.Set(data, count);
                        view.gameObject.SetActive(true);
                    }
                }
            }

            if (battleRewards.Materials is not null && battleRewards.Materials.Any())
            {
                foreach (var reward in battleRewards.Materials)
                {
                    var data = new CountableItem(reward.Key, reward.Value);
                    var view = itemRewardViews.First(x => !x.gameObject.activeSelf);
                    view.SetData(data);
                    view.Show();
                }
            }

            RefreshSeasonPassCourageAmount();
        }

        public void ShowAsPractice(int bossId, long score)
        {
            base.Show();
            AudioController.instance.PlayMusic(AudioController.MusicCode.WorldBossBattleResult);
            _practiceText.SetActive(true);
            scoreText.text = score.ToString("N0");
            seasonBestObject.SetActive(false);

            foreach (var view in runeRewardViews)
            {
                view.gameObject.SetActive(false);
            }

            foreach (var item in itemRewardViews)
            {
                item.Hide();
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
                Find<WorldBossRewardScreen>().Show(_killRewards, () => { Find<WorldBoss>().ShowAsync().Forget(); });
            }
            else
            {
                Find<WorldBoss>().ShowAsync().Forget();
            }

            base.Close(ignoreCloseAnimation);
        }

        private void RefreshSeasonPassCourageAmount()
        {
            var seasonPassServiceManager = ApiClients.Instance.SeasonPassServiceManager;
            if (seasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }
                var expAmount = seasonPassServiceManager.ExpPointAmount(SeasonPassServiceClient.PassType.CouragePass, SeasonPassServiceClient.ActionType.raid);
                seasonPassCourageAmount.text = $"+{expAmount}";
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
