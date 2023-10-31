using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Nekoyume.UI.Module;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;
using Nekoyume.Model.Mail;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;

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
        private Image expLineImage;
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
                expLineImage.fillAmount = (float)(seasonPassInfo.Exp - minExp) / (float)(maxExp - minExp);

                receiveBtn.Interactable = seasonPassInfo.Level > seasonPassInfo.LastNormalClaim;

                lineImage.fillAmount = (float)(seasonPassInfo.Level - 1) / (float)(seasonPassManager.CurrentSeasonPassData.RewardList.Count-1);

                premiumIcon.SetActive(!seasonPassInfo.IsPremiumPlus);
                premiumUnlockBtn.SetActive(!seasonPassInfo.IsPremium);
                premiumPlusUnlockBtn.SetActive(seasonPassInfo.IsPremium && !seasonPassInfo.IsPremiumPlus);
                premiumPlusIcon.SetActive(seasonPassInfo.IsPremiumPlus);
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

        public void ShowSeasonPassPremiumPopup()
        {
#if UNITY_ANDROID || UNITY_IOS
            Widget.Find<SeasonPassPremiumPopup>().Show();
#else
            var confirm = Widget.Find<ConfirmPopup>();
            confirm.CloseCallback = result =>
            {
                var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
                switch (result)
                {
                    case ConfirmResult.Yes:
                        Application.OpenURL(seasonPassManager.GoogleMarketURL);
                        break;
                    case ConfirmResult.No:
                        Application.OpenURL(seasonPassManager.AppleMarketURL);
                        break;
                    default:
                        break;
                }
            };
            confirm.Show("UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_TITLE", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_CONTENT", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_ANDROID", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_IOS");
#endif
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarStateRefresh().AsUniTask().Forget();

            rewardCellScrollbar.value = (float)(seasonPassManager.AvatarInfo.Value.Level - 1) / (float)(seasonPassManager.CurrentSeasonPassData.RewardList.Count - 1);
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
            receiveBtn.Interactable = false;
            Game.Game.instance.SeasonPassServiceManager.ReceiveAll(
                (result) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"),NotificationCell.NotificationType.Notification);
                    Game.Game.instance.SeasonPassServiceManager.AvatarStateRefresh().AsUniTask().Forget();
                },
                (error) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), NotificationCell.NotificationType.Notification);
                });
        }
    }
}
