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
using DG.Tweening;

namespace Nekoyume.UI
{
    public class SeasonPass : Widget
    {
        [SerializeField]
        private ConditionalButton receiveBtn;
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
        [SerializeField]
        private ConditionalButton prevSeasonClaimButton;
        [SerializeField]
        private TextMeshProUGUI prevSeasonClaimButtonRemainingText;

        private bool isPageEffectComplete;
        public const int SeasonPassMaxLevel = 30;
        public const string MaxLevelString = "30";
        private int popupViewDelay = 1200;

        protected override void Awake()
        {
            base.Awake();
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            seasonPassManager.AvatarInfo.Subscribe((seasonPassInfo) => {
                if (seasonPassInfo == null)
                    return;

                seasonPassManager.GetExp(seasonPassInfo.Level, out var minExp, out var maxExp);

                if (seasonPassInfo.Level >= SeasonPassMaxLevel)
                {
                    levelText.text = MaxLevelString;
                    expText.text = $"{seasonPassInfo.Exp - minExp} / {maxExp - minExp}";
                    expLineImage.fillAmount = (float)(seasonPassInfo.Exp - minExp) / (float)(maxExp - minExp);
                }
                else
                {
                    levelText.text = seasonPassInfo.Level.ToString();
                    expText.text = $"{seasonPassInfo.Exp - minExp} / {maxExp - minExp}";
                    expLineImage.fillAmount = (float)(seasonPassInfo.Exp - minExp) / (float)(maxExp - minExp);
                }
                lastRewardCell.SetData(seasonPassManager.CurrentSeasonPassData.RewardList[SeasonPassMaxLevel]);
                receiveBtn.Interactable = seasonPassInfo.Level > seasonPassInfo.LastNormalClaim
                    || (seasonPassInfo.IsPremium && seasonPassInfo.Level > seasonPassInfo.LastPremiumClaim);

                lineImage.fillAmount = (float)(seasonPassInfo.Level - 1) / (float)(seasonPassManager.CurrentSeasonPassData.RewardList.Count - 2);

                premiumIcon.SetActive(!seasonPassInfo.IsPremiumPlus);
                premiumUnlockBtn.SetActive(!seasonPassInfo.IsPremium);
                premiumPlusUnlockBtn.SetActive(seasonPassInfo.IsPremium && !seasonPassInfo.IsPremiumPlus);
                premiumPlusIcon.SetActive(seasonPassInfo.IsPremiumPlus);


            }).AddTo(gameObject);

            seasonPassManager.RemainingDateTime.Subscribe((endDate) =>
            {
                remainingText.text = endDate;
            });

            seasonPassManager.SeasonEndDate.Subscribe((endTime) =>
            {
                RefreshRewardCells(seasonPassManager);
            }).AddTo(gameObject);

            seasonPassManager.PrevSeasonClaimAvailable.Subscribe(visible =>
            {
                prevSeasonClaimButton.gameObject.SetActive(visible);
            }).AddTo(gameObject);

            seasonPassManager.PrevSeasonClaimRemainingDateTime.Subscribe(remaining =>
            {
                prevSeasonClaimButtonRemainingText.text = remaining;
            }).AddTo(gameObject);

            rewardCellScrollbar.value = 0;
        }

        private void RefreshRewardCells(SeasonPassServiceManager seasonPassManager)
        {
            if (seasonPassManager.CurrentSeasonPassData == null)
            {
                NcDebug.LogError("[RefreshRewardCells] RefreshFailed");
                return;
            }

            for (int i = 0; i < rewardCells.Length; i++)
            {
                if (i < seasonPassManager.CurrentSeasonPassData.RewardList.Count)
                {
                    rewardCells[i].gameObject.SetActive(true);
                    rewardCells[i].SetData(seasonPassManager.CurrentSeasonPassData.RewardList[i]);
                }
                else
                {
                    rewardCells[i].gameObject.SetActive(false);
                }
            }
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
            seasonPassManager.AvatarStateRefreshAsync().AsUniTask().Forget();

            RefreshRewardCells(seasonPassManager);

            if(!ignoreShowAnimation)
                PageEffect();

            if (!PlayerPrefs.HasKey(seasonPassManager.GetSeassonPassPopupViewKey()))
            {
                async UniTaskVoid ShowCellEffect()
                {
                    await UniTask.Delay(popupViewDelay);
                    Find<SeasonPassCouragePopup>().Show();
                    PlayerPrefs.SetInt(seasonPassManager.GetSeassonPassPopupViewKey(), 1);
                }
                ShowCellEffect().Forget();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            rewardCellScrollbar.value = 0;
        }

        [SerializeField]
        private int betweenCellViewDuration = 60;
        [SerializeField]
        private float scrollDuration = 1f;
        [SerializeField]
        private int scrollWaitDuration = 300;
        [SerializeField]
        private int miniumDurationCount = 400;
        [ContextMenu("ShowEffect")]
        public void PageEffect()
        {
            isPageEffectComplete = false;
            rewardCellScrollbar.value = 0;

            async UniTaskVoid ShowCellEffect()
            {

                var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
                var cellIndex = Mathf.Max(0, seasonPassManager.AvatarInfo.Value.Level - 1);

                var tween = DOTween.To(() => rewardCellScrollbar.value,
                    value => rewardCellScrollbar.value = value, CalculateScrollerStartPosition(), scrollDuration).SetEase(Ease.OutQuart);
                tween.Play();

                for (int i = cellIndex; i < rewardCells.Length; i++)
                {
                    rewardCells[i].SetTweeningStarting();
                }

                await UniTask.Delay(scrollWaitDuration);

                int durationCount = 0;
                for (int i = cellIndex; i < rewardCells.Length; i++)
                {
                    rewardCells[i].ShowTweening();
                    await UniTask.Delay(betweenCellViewDuration);
                    durationCount += betweenCellViewDuration;
                    if(durationCount > miniumDurationCount)
                        isPageEffectComplete = true;
                }
                isPageEffectComplete = true;
            }
            ShowCellEffect().Forget();
        }

        public float CalculateScrollerStartPosition()
        {
            var seasonPassManager = Game.Game.instance.SeasonPassServiceManager;
            float totalScrollbarLength = 3500f;
            float paddingLeft = 20f;
            float viewSize = rewardCellScrollbar.GetComponent<RectTransform>().rect.width;
            int currentLevel = Mathf.Max(0, seasonPassManager.AvatarInfo.Value.Level - 1);
            float levelWidth = 110;
            float usableLength = totalScrollbarLength - viewSize;

            float currentPosition = paddingLeft + (levelWidth * currentLevel) - 10;

            float value = currentPosition / usableLength;

            return Mathf.Min(value, 1);
        }

        public void ReceiveAllBtn()
        {
            receiveBtn.Interactable = false;
            Game.Game.instance.SeasonPassServiceManager.ReceiveAll(
                (result) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"),NotificationCell.NotificationType.Notification);
                    Game.Game.instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
                },
                (error) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), NotificationCell.NotificationType.Notification);
                });
        }

        public void PrevSeasonClaim()
        {
            prevSeasonClaimButton.SetConditionalState(false);
            
            Game.Game.instance.SeasonPassServiceManager.PrevClaim(
                result =>
                {
                    OneLineSystem.Push(
                        MailType.System, 
                        L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"), 
                        NotificationCell.NotificationType.Notification);
                    Game.Game.instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
                    prevSeasonClaimButton.SetConditionalState(true);
                    prevSeasonClaimButton.gameObject.SetActive(false);
                },
                error =>
                {
                    OneLineSystem.Push(
                        MailType.System, 
                        L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), 
                        NotificationCell.NotificationType.Notification);
                    prevSeasonClaimButton.SetConditionalState(true);
                });
        }
    }
}
