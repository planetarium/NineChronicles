using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nekoyume.UI.Module;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Nekoyume.Model.Mail;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;
using DG.Tweening;
using Nekoyume.ApiClient;
using System;

namespace Nekoyume.UI
{
    using Coffee.UIEffects;
    using System.Linq;
    using System.Threading;
    using UniRx;
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
        private List<SeasonPassRewardCell> rewardCells;

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

        [SerializeField]
        private RectTransform scrollContents;

        [SerializeField]
        private HorizontalLayoutGroup scrollHorizontalLayout;

        [Serializable]
        private struct SeasonPassUiInfoByType
        {
            public SeasonPassServiceClient.PassType Type;
            public Sprite BackGroundSprite;
            public Sprite IconNormalSprite;
            public Sprite IconPremiumSprite;
            public Sprite IconPremiumPlusSprite;
            public Sprite levelBackGroundSprite;
            public Color LevelTextColor;
            public Color SlideLeft;
            public Color SlideRight;
        }

        [SerializeField]
        private SeasonPassUiInfoByType[] seasonPassUiInfoByType;

        [SerializeField]
        private Image iconNormalImage;
        [SerializeField]
        private Image iconPremiumImage;
        [SerializeField]
        private Image iconPremiumPlusImage;
        [SerializeField]
        private Image backGroundImage;
        [SerializeField]
        private Image levelBackGroundImage;
        [SerializeField]
        private TextMeshProUGUI seasonPassTypeName;

        [SerializeField]
        private GameObject[] remainingTimeObject;

        [SerializeField]
        private UI.Module.Toggle[] categoryToggles;

        [SerializeField]
        private GameObject courageIcon;
        [SerializeField]
        private TextMeshProUGUI levelNameText;
        [SerializeField]
        private UIGradient sliderGradient;

        [SerializeField]
        private GameObject CourageRedDot;
        [SerializeField]
        private GameObject WorldClearRedDot;
        [SerializeField]
        private GameObject AdventureBossRedDot;

        [SerializeField]
        private GameObject[] objectsToDisableWhenNoSeason;
        [SerializeField]
        private GameObject[] objectsToEnableWhenNoSeason;

        [SerializeField]
        private TextMeshProUGUI seasonPassDescText;

        private RectTransform lineImageRectTransform;
        private float rewardCellWidth;
        private float lastRewardCellWidth;

        public const int SeasonPassMaxLevel = 30;
        public const string MaxLevelString = "30";
        private int popupViewDelay = 1200;

        private SeasonPassServiceClient.PassType currentSeasonPassType = SeasonPassServiceClient.PassType.CouragePass;

        protected override void Awake()
        {
            lineImageRectTransform = lineImage.GetComponent<RectTransform>();
            rewardCellWidth = rewardCells[0].GetComponent<RectTransform>().rect.width;
            lastRewardCellWidth = lastRewardCell.GetComponent<RectTransform>().rect.width;
            base.Awake();
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;

            seasonPassManager.RemainingDateTime.Subscribe((endDate) => { remainingText.text = endDate; });

            rewardCellScrollbar.value = 0;
        }

        private void RefreshPrevSeasonClaimButton(SeasonPassServiceClient.PassType passType)
        {
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            if (seasonPassManager.HasPrevClaimPassType.Contains(passType))
            {
                prevSeasonClaimButton.gameObject.SetActive(true);
            }
            else
            {
                prevSeasonClaimButton.gameObject.SetActive(false);
            }

            prevSeasonClaimButtonRemainingText.text = seasonPassManager.GetPrevRemainingClaim(passType);
        }

        private SeasonPassUiInfoByType GetSeasonPassUiInfoByType(SeasonPassServiceClient.PassType type)
        {
            foreach (var info in seasonPassUiInfoByType)
            {
                if (info.Type == type)
                {
                    return info;
                }
            }
            Debug.LogError($"Not found SeasonPassUiInfoByType: {type}");
            return default;
        }

        private void RefreshRewardCells(List<SeasonPassServiceClient.RewardSchema> rewardSchemas, bool existLastCell = true)
        {
            AddRewardCellIfNeeded(rewardSchemas.Count);

            for (var i = 0; i < rewardCells.Count; i++)
            {
                if (i < rewardSchemas.Count)
                {
                    rewardCells[i].gameObject.SetActive(true);
                    rewardCells[i].SetData(rewardSchemas[i], currentSeasonPassType);
                }
                else
                {
                    rewardCells[i].gameObject.SetActive(false);
                }
            }
            lastRewardCell.gameObject.SetActive(existLastCell);
            if (existLastCell)
            {
                rewardCells[rewardSchemas.Count - 1].gameObject.SetActive(false);
                lastRewardCell.SetData(rewardSchemas.Last(), currentSeasonPassType);
            }

            scrollHorizontalLayout.CalculateLayoutInputHorizontal();
            scrollContents.sizeDelta = new Vector2(scrollHorizontalLayout.preferredWidth, scrollContents.sizeDelta.y);
            CalculateLineImageWith(existLastCell);
        }

        /// <summary>
        /// 필요한 수의 리워드 셀을 확보합니다.
        /// 리스트에 관리되지 않는 마지막 차일드가 있으므로, 새로운 셀은 앞쪽에 추가됩니다.
        /// </summary>
        /// <param name="rewardsCount"></param>
        private void AddRewardCellIfNeeded(int rewardsCount)
        {
            //rewardsCount count 체크 후 부족한 만큼 생성
            if (rewardsCount > rewardCells.Count)
            {
                var startCount = rewardCells.Count;
                for (var i = startCount; i < rewardsCount; i++)
                {
                    var cell = Instantiate(rewardCells[0], rewardCells[0].transform.parent);
                    cell.name = $"RewardCell_{i}";
                    cell.transform.SetAsFirstSibling();
                    rewardCells.Insert(0, cell);
                }
            }
        }

        public void ShowSeasonPassPremiumPopup()
        {
#if UNITY_ANDROID || UNITY_IOS
            Widget.Find<SeasonPassPremiumPopup>().Show(currentSeasonPassType);
#else
            var confirm = Find<ConfirmPopup>();
            confirm.CloseCallback = result =>
            {
                var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
                switch (result)
                {
                    case ConfirmResult.Yes:
                        Helper.Util.OpenURL(seasonPassManager.GoogleMarketURL);
                        break;
                    case ConfirmResult.No:
                        Helper.Util.OpenURL(seasonPassManager.AppleMarketURL);
                        break;
                    default:
                        break;
                }
            };
            confirm.Show("UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_TITLE", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_CONTENT", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_ANDROID", "UI_CONFIRM_SEASONPASS_UNLOCK_FAIL_IOS");
#endif
        }

        public void Show(SeasonPassServiceClient.PassType seasonPassType = SeasonPassServiceClient.PassType.CouragePass, bool ignoreShowAnimation = false)
        {
            Find<LoadingScreen>().Show();
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            seasonPassManager.AvatarStateRefreshAsync().AsUniTask().ContinueWith(() =>
            {
                Find<LoadingScreen>().Close();
                base.Show(ignoreShowAnimation);
                RefreshRedDotObj();
                categoryToggles[(int)seasonPassType].isOn = true;
                categoryToggles[(int)seasonPassType].onClickToggle.Invoke();

                if (!PlayerPrefs.HasKey(seasonPassManager.GetSeasonPassPopupViewKey()))
                {
                    async UniTaskVoid AwaitSeasonPassPopup()
                    {
                        await UniTask.Delay(popupViewDelay);
                        Find<SeasonPassNewPopup>().Show();
                        PlayerPrefs.SetInt(seasonPassManager.GetSeasonPassPopupViewKey(), 1);
                    }

                    AwaitSeasonPassPopup().Forget();
                }
            }).Forget();
        }

        public void OnClickSeasonPassToggle(int index)
        {
            var type = (SeasonPassServiceClient.PassType)index;
            ChangePageByType(type);
        }

        public void ChangePageByType(SeasonPassServiceClient.PassType type)
        {
            currentSeasonPassType = type;
            var info = GetSeasonPassUiInfoByType(type);
            backGroundImage.sprite = info.BackGroundSprite;
            backGroundImage.SetNativeSize();
            iconNormalImage.sprite = info.IconNormalSprite;
            iconNormalImage.SetNativeSize();
            iconPremiumImage.sprite = info.IconPremiumSprite;
            iconPremiumImage.SetNativeSize();
            iconPremiumPlusImage.sprite = info.IconPremiumPlusSprite;
            iconPremiumPlusImage.SetNativeSize();
            levelBackGroundImage.sprite = info.levelBackGroundSprite;
            levelNameText.color = info.LevelTextColor;
            sliderGradient.color1 = info.SlideLeft;
            sliderGradient.color2 = info.SlideRight;

            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            int currentLevel = 0;
            if (seasonPassManager.UserSeasonPassDatas.TryGetValue(type, out var userSeasonPassData))
            {
                currentLevel = userSeasonPassData.Level;
            }
            else
            {
                NcDebug.LogError($"Not found UserSeasonPassData: {type}");
            }

            List<SeasonPassServiceClient.RewardSchema> rewardListData = new List<SeasonPassServiceClient.RewardSchema>();
            bool existLastCell = false;
            if (seasonPassManager.CurrentSeasonPassData.TryGetValue(type, out var seasonPassSchema))
            {
                rewardListData = seasonPassSchema.RewardList;
                existLastCell = seasonPassSchema.RepeatLastReward;
                UpdateSeasonalObjects(true);
            }
            else
            {
                //시즌정보가 없을때 준비중 UI 준비
                UpdateSeasonalObjects(false);
            }

            int minExp;
            int maxExp;
            // 타입별 데이터 분기처리 예정.
            string expL10nKey = "UI_SEASONPASS_COURAGE_EXP";
            switch (type)
            {
                case SeasonPassServiceClient.PassType.CouragePass:
                    seasonPassTypeName.text = L10nManager.Localize("UI_SEASONPASS_COURAGE");
                    foreach (var obj in remainingTimeObject)
                    {
                        obj.SetActive(true);
                    }
                    courageIcon.SetActive(true);
                    expL10nKey = "UI_SEASONPASS_COURAGE_EXP";
                    seasonPassDescText.text = L10nManager.Localize("UI_SEASONPASS_COURAGE_DESC");
                    break;
                case SeasonPassServiceClient.PassType.WorldClearPass:
                    seasonPassTypeName.text = L10nManager.Localize("UI_SEASONPASS_WORLD_CLEAR");
                    foreach (var obj in remainingTimeObject)
                    {
                        obj.SetActive(false);
                    }
                    courageIcon.SetActive(false);
                    expL10nKey = "UI_SEASONPASS_WORLD_CLEAR_EXP";
                    seasonPassDescText.text = L10nManager.Localize("UI_SEASONPASS_WORLD_CLEAR_DESC");
                    break;
                case SeasonPassServiceClient.PassType.AdventureBossPass:
                    seasonPassTypeName.text = L10nManager.Localize("UI_SEASONPASS_ADVENTUREBOSS");
                    foreach (var obj in remainingTimeObject)
                    {
                        obj.SetActive(true);
                    }
                    courageIcon.SetActive(false);
                    expL10nKey = "UI_SEASONPASS_ADVENTUREBOSS_EXP";
                    seasonPassDescText.text = L10nManager.Localize("UI_SEASONPASS_ADVENTUREBOSS_DESC");
                    break;
                default:
                    NcDebug.LogError($"Not found SeasonPassType: {type}");
                    break;
            }
            seasonPassManager.GetExp(type, currentLevel, out minExp, out maxExp);

            if(userSeasonPassData != null)
            {
                //최대래밸 고정
                levelText.text = Mathf.Min(userSeasonPassData.Level, rewardListData.Count).ToString();
                expText.text = L10nManager.Localize(expL10nKey, userSeasonPassData.Exp - minExp, maxExp - minExp);
                expLineImage.fillAmount = (float)(userSeasonPassData.Exp - minExp) / (float)(maxExp - minExp);
                receiveBtn.Interactable = userSeasonPassData.Level > userSeasonPassData.LastNormalClaim
                    || (userSeasonPassData.IsPremium && userSeasonPassData.Level > userSeasonPassData.LastPremiumClaim);

                premiumIcon.SetActive(!userSeasonPassData.IsPremiumPlus);
                premiumUnlockBtn.SetActive(!userSeasonPassData.IsPremium);
                premiumPlusUnlockBtn.SetActive(userSeasonPassData.IsPremium && !userSeasonPassData.IsPremiumPlus);
                premiumPlusIcon.SetActive(userSeasonPassData.IsPremiumPlus);
            }
            else
            {
                levelText.text = "0";
                expText.text = L10nManager.Localize(expL10nKey, 0, maxExp - minExp);
                expLineImage.fillAmount = 0;
                receiveBtn.Interactable = false;

                premiumIcon.SetActive(true);
                premiumUnlockBtn.SetActive(true);
                premiumPlusUnlockBtn.SetActive(false);
                premiumPlusIcon.SetActive(false);
            }

            // 보상 샐 데이터 갱신
            RefreshRewardCells(rewardListData, existLastCell);

            // 라인 이미지 채우는 비율 계산 (현재 레벨 - 1) / (총 셀 갯수 - 1) 래벨이 1부터 시작임을 가정.
            if(rewardListData.Count > 2 )
            {
                var lastIndexAdjuster = existLastCell ? 2 : 1;
                lineImage.fillAmount = (float)(currentLevel - 1) / (float)(rewardListData.Count - lastIndexAdjuster);
            }

            // 현재 래밸까지 스크롤 이동 연출
            rewardCellScrollbar.value = 0;
            var cellIndex = Mathf.Max(0, currentLevel - 1);
            RefreshPrevSeasonClaimButton(type);
            ShowCellEffect(cellIndex).Forget();
        }

        private void UpdateSeasonalObjects(bool isSeasonActive)
        {
            foreach (var obj in objectsToDisableWhenNoSeason)
            {
                obj.SetActive(isSeasonActive);
            }
            foreach (var obj in objectsToEnableWhenNoSeason)
            {
                obj.SetActive(!isSeasonActive);
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

        private CancellationTokenSource _cts;

        private async UniTaskVoid ShowCellEffect(int cellIndex)
        {
            // 기존 작업이 있다면 취소하고 새 토큰으로 갱신
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            var tween = DOTween.To(() => rewardCellScrollbar.value,
                value => rewardCellScrollbar.value = value, CalculateScrollerStartPosition(cellIndex), scrollDuration).SetEase(Ease.OutQuart);
            rewardCellScrollbar.value = 0;
            tween.Play();
            // 트윈이 취소될 수 있도록 토큰 감시
            token.Register(() => tween.Kill());

            for (var i = 0; i < rewardCells.Count; i++)
            {
                rewardCells[i].SetTweeningStarting(i < cellIndex);
            }

            try
            {
                await UniTask.Delay(scrollWaitDuration, cancellationToken: token);

                var durationCount = 0;
                for (var i = cellIndex; i < rewardCells.Count; i++)
                {
                    rewardCells[i].ShowTweening();
                    await UniTask.Delay(betweenCellViewDuration, cancellationToken: token);
                    durationCount += betweenCellViewDuration;
                }
            }
            catch (OperationCanceledException)
            {
                // 토큰이 취소되면 무시
                Debug.Log("ShowCellEffect Cancelled");
            }
        }

        public float CalculateScrollerStartPosition(int currentLevel)
        {
            // 총 스크롤바 길이 계산 (컨텐츠의 너비 * 컨텐츠의 스케일)
            var totalScrollbarLength = scrollContents.sizeDelta.x * scrollContents.localScale.x;
            var paddingLeft = scrollHorizontalLayout.padding.left;
            var viewSize = rewardCellScrollbar.GetComponent<RectTransform>().rect.width;
            var usableLength = totalScrollbarLength - viewSize;

            //현재 레벨의 셀의 위치 계산 (paddingLeft + ((셀의 너비 + 셀간격) * 현재레벨) - 추가 조정 픽셀)
            var currentPosition = paddingLeft + ((rewardCellWidth + scrollHorizontalLayout.spacing) * currentLevel) - 10;

            // 스케일을 곱해줘야함 scrollContents의 스케일이 1이 아닐 수 있기 때문(레이아웃 변경으로 현재 프리팹의 스케일이 0.9)
            currentPosition *= scrollContents.localScale.x;

            var value = currentPosition / usableLength;

            return Mathf.Min(value, 1);
        }

        public void ReceiveAllBtn()
        {
            receiveBtn.Interactable = false;
            ApiClients.Instance.SeasonPassServiceManager.ReceiveAll(currentSeasonPassType,
                (result) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"), NotificationCell.NotificationType.Notification);
                    ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().ContinueWith(() =>
                    {
                        RefreshCurrentPage();
                    }).Forget();
                },
                (error) => { OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), NotificationCell.NotificationType.Notification); });
        }

        public void RefreshCurrentPage()
        {
            ChangePageByType(currentSeasonPassType);
            RefreshRedDotObj();
        }

        public void RefreshRedDotObj()
        {
            //레드닷 갱신
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            CourageRedDot.SetActive(seasonPassManager.HasClaimPassType.Contains(SeasonPassServiceClient.PassType.CouragePass) || seasonPassManager.HasPrevClaimPassType.Contains(SeasonPassServiceClient.PassType.CouragePass));
            WorldClearRedDot.SetActive(seasonPassManager.HasClaimPassType.Contains(SeasonPassServiceClient.PassType.WorldClearPass) || seasonPassManager.HasPrevClaimPassType.Contains(SeasonPassServiceClient.PassType.WorldClearPass));
            AdventureBossRedDot.SetActive(seasonPassManager.HasClaimPassType.Contains(SeasonPassServiceClient.PassType.AdventureBossPass) || seasonPassManager.HasPrevClaimPassType.Contains(SeasonPassServiceClient.PassType.AdventureBossPass));
        }

        public void PrevSeasonClaim()
        {
            prevSeasonClaimButton.SetConditionalState(false);

            ApiClients.Instance.SeasonPassServiceManager.PrevClaim(currentSeasonPassType,
                result =>
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"),
                        NotificationCell.NotificationType.Notification);
                    ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().ContinueWith(() =>
                    {
                        RefreshCurrentPage();
                    }).Forget();
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

        /// <summary>
        /// 라인 이미지의 너비 계산
        /// </summary>
        private void CalculateLineImageWith(bool existLastCell)
        {
            // 마지막 셀의 위치 계산 (paddingRight + 셀의 너비 / 2)
            var lastLineImagePositon = scrollHorizontalLayout.padding.right + (rewardCellWidth / 2);

            // 마지막 셀이 존재하는 경우 마지막 셀의 너비를 더해줌 (셀간격도 더해줌)
            if (existLastCell)
            {
                lastLineImagePositon += lastRewardCellWidth + scrollHorizontalLayout.spacing;
            }

            // 계산한크기만큼 빼줘야 스크롤뷰 안에 위치함
            lastLineImagePositon = -lastLineImagePositon;

            var offsetMax = lineImageRectTransform.offsetMax;
            offsetMax.x = lastLineImagePositon;
            lineImageRectTransform.offsetMax = offsetMax;
        }
    }
}
