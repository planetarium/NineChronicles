using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Nekoyume.UI.Module;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace Nekoyume.UI
{
    public class SeasonPass : Widget
    {
        [SerializeField]
        private ConditionalButton receiveBtn;
        [SerializeField]
        private Animator lastRewardAnim;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI remainingText;
        [SerializeField]
        private TextMeshProUGUI expText;
        [SerializeField]
        private SeasonPassRewardCell[] rewardCells;
        [SerializeField]
        private Image lineImage;
        [SerializeField]
        private SeasonPassRewardCell lastRewardCell;
        [SerializeField]
        private Scrollbar rewardCellScrollbar;
        [SerializeField]
        private Slider expSlider;
        [SerializeField]
        private GameObject premiumIcon;
        [SerializeField]
        private GameObject premiumUnlockBtn;
        [SerializeField]
        private GameObject premiumPlusUnlockBtn;
        [SerializeField]
        private GameObject premiumPlusIcon;

        private bool isLastCellShow;

        protected override void Awake()
        {
            base.Awake();
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarInfo.Subscribe((seasonPassInfo) => {
                if(seasonPassInfo == null)
                    return;

                levelText.text = seasonPassInfo.Level.ToString();
                seasonPassManager.GetExp(seasonPassInfo.Level, out var minExp, out var maxExp);
                expText.text = $"{seasonPassInfo.Exp - minExp} / {maxExp - minExp}";
                expSlider.value = (float)(seasonPassInfo.Exp - minExp) / (float)(maxExp - minExp);

                receiveBtn.Interactable = seasonPassInfo.Level > seasonPassInfo.LastNormalClaim;

                lineImage.fillAmount = (float)(seasonPassInfo.Level - 1f) / (float)seasonPassManager.CurrentSeasonPassData.RewardList.Count;

                /*premiumIcon.SetActive(!seasonPassInfo.IsPremiumPlus);*/
                premiumUnlockBtn.SetActive(!seasonPassInfo.IsPremium);
                /*premiumPlusUnlockBtn.SetActive(seasonPassInfo.IsPremium && !seasonPassInfo.IsPremiumPlus);*/
                /*premiumPlusIcon.SetActive(seasonPassInfo.IsPremiumPlus);*/
            }).AddTo(gameObject);

            seasonPassManager.RemainingDateTime.Subscribe((endDate) =>
            {
                remainingText.text = $"Remaining Time <Style=Clock> {endDate}";
            });

            seasonPassManager.SeasonEndDate.Subscribe((endTime) =>
            {
                for (int i = 0; i < rewardCells.Length; i++)
                {
                    if(i < seasonPassManager.CurrentSeasonPassData.RewardList.Count)
                    {
                        rewardCells[i].gameObject.SetActive(true);
                        rewardCells[i].SetData(seasonPassManager.CurrentSeasonPassData.RewardList[i]);
                    }
                    else
                    {
                        rewardCells[i].gameObject.SetActive(false);
                    }
                }
                lastRewardCell.SetData(seasonPassManager.CurrentSeasonPassData.RewardList.Last());
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Game.Game.instance.SeasonPassServiceManager.AvatarStateRefresh().AsUniTask().Forget();
        }

        protected override void Update()
        {
            base.Update();
            if(rewardCellScrollbar.value > 0.95f)
            {
                if (isLastCellShow)
                {
                    isLastCellShow = false;
                    lastRewardAnim.Play("SeasonPassLastReward@Close", -1);
                }
            }
            else
            {
                if (!isLastCellShow)
                {
                    lastRewardAnim.Play("SeasonPassLastReward@Show", -1);
                    isLastCellShow = true;
                }
            }
        }

        public void ReceiveAllBtn()
        {
            Game.Game.instance.SeasonPassServiceManager.ReceiveAll(
                (result) =>
                {
                    Debug.Log($"SeasonPass ReceiveSuccess~!! {result.User.AvatarAddr} {result.Items.Count} {result.Currencies.Count}");
                    //result.
                    Game.Game.instance.SeasonPassServiceManager.AvatarStateRefresh().AsUniTask().Forget();
                },
                (error) =>
                {

                });
        }
    }
}
