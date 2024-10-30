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

        public enum SeasonPassType
        {
            Courage,
            WorldClear,
            AdventureBoss
        }

        [Serializable]
        private struct SeasonPassUiInfoByType
        {
            public SeasonPassType Type;
            public Sprite BackGroundSprite;
            public Sprite IconNormalSprite;
            public Sprite IconPremiumSprite;
            public Sprite IconPremiumPlusSprite;
            public Sprite levelBackGroundSprite;
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

        private RectTransform lineImageRectTransform;
        private float rewardCellWidth;
        private float lastRewardCellWidth;

        public const int SeasonPassMaxLevel = 30;
        public const string MaxLevelString = "30";
        private int popupViewDelay = 1200;

        private SeasonPassType currentSeasonPassType = SeasonPassType.Courage;

        protected override void Awake()
        {
            lineImageRectTransform = lineImage.GetComponent<RectTransform>();
            rewardCellWidth = rewardCells[0].GetComponent<RectTransform>().rect.width;
            lastRewardCellWidth = lastRewardCell.GetComponent<RectTransform>().rect.width;
            base.Awake();
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            seasonPassManager.RemainingDateTime.Subscribe((endDate) => { remainingText.text = endDate; });
            seasonPassManager.SeasonEndDate.Subscribe((endTime) =>
            {
                if (seasonPassManager.CurrentSeasonPassData == null)
                {
                    NcDebug.LogError("[RefreshRewardCells] RefreshFailed");
                    return;
                }
                RefreshRewardCells(seasonPassManager.CurrentSeasonPassData.RewardList);
            }).AddTo(gameObject);
            seasonPassManager.PrevSeasonClaimAvailable.Subscribe(visible => { prevSeasonClaimButton.gameObject.SetActive(visible); }).AddTo(gameObject);
            seasonPassManager.PrevSeasonClaimRemainingDateTime.Subscribe(remaining => { prevSeasonClaimButtonRemainingText.text = remaining; }).AddTo(gameObject);

            rewardCellScrollbar.value = 0;
        }

        private SeasonPassUiInfoByType GetSeasonPassUiInfoByType(SeasonPassType type)
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
                    if(currentSeasonPassType == SeasonPassType.Courage)
                    {
                        rewardCells[i].SetData(rewardSchemas[i]);
                    }
                    else
                    {
                        rewardCells[i].SetLevelText(i + 1);
                    }
                }
                else
                {
                    rewardCells[i].gameObject.SetActive(false);
                }
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
            Widget.Find<SeasonPassPremiumPopup>().Show();
#else
            var confirm = Find<ConfirmPopup>();
            confirm.CloseCallback = result =>
            {
                var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
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

        public void Show(SeasonPassType seasonPassType = SeasonPassType.Courage, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            seasonPassManager.AvatarStateRefreshAsync().AsUniTask().Forget();

            ChangePageByType(seasonPassType);

            if (!PlayerPrefs.HasKey(seasonPassManager.GetSeasonPassPopupViewKey()))
            {
                async UniTaskVoid AwaitSeasonPassPopup()
                {
                    await UniTask.Delay(popupViewDelay);
                    Find<SeasonPassCouragePopup>().Show();
                    PlayerPrefs.SetInt(seasonPassManager.GetSeasonPassPopupViewKey(), 1);
                }

                AwaitSeasonPassPopup().Forget();
            }
        }

        public void OnClickSeasonPassToggle(int index)
        {
            var type = (SeasonPassType)index;
            ChangePageByType(type);
        }

        public void ChangePageByType(SeasonPassType type)
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

            var seasonPassManager = ApiClients.Instance.SeasonPassServiceManager;
            SeasonPassServiceClient.UserSeasonPassSchema seasonPassAvatarInfo = seasonPassManager.AvatarInfo.Value;
            List<SeasonPassServiceClient.RewardSchema> rewardListData;
            int currentLevel = seasonPassManager.AvatarInfo.Value.Level;
            int minExp;
            int maxExp;
            bool existLastCell = false;
            // 타입별 데이터 분기처리 예정.
            switch (type)
            {
                case SeasonPassType.Courage:
                    rewardListData = seasonPassManager.CurrentSeasonPassData.RewardList;
                    existLastCell = true;
                    break;
                case SeasonPassType.WorldClear:
                    rewardListData = new List<SeasonPassServiceClient.RewardSchema>(15);
                    existLastCell = false;
                    break;
                case SeasonPassType.AdventureBoss:
                    rewardListData = new List<SeasonPassServiceClient.RewardSchema>(40);
                    existLastCell = true;
                    break;
                default:
                    rewardListData = seasonPassManager.CurrentSeasonPassData.RewardList;
                    break;
            }
            seasonPassManager.GetExp(currentLevel, out minExp, out maxExp);

            //최대래밸 고정
            levelText.text = Mathf.Min(seasonPassAvatarInfo.Level, rewardListData.Count).ToString();
            expText.text = $"{seasonPassAvatarInfo.Exp - minExp} / {maxExp - minExp}";
            expLineImage.fillAmount = (float)(seasonPassAvatarInfo.Exp - minExp) / (float)(maxExp - minExp);
            receiveBtn.Interactable = seasonPassAvatarInfo.Level > seasonPassAvatarInfo.LastNormalClaim
                || (seasonPassAvatarInfo.IsPremium && seasonPassAvatarInfo.Level > seasonPassAvatarInfo.LastPremiumClaim);

            premiumIcon.SetActive(!seasonPassAvatarInfo.IsPremiumPlus);
            premiumUnlockBtn.SetActive(!seasonPassAvatarInfo.IsPremium);
            premiumPlusUnlockBtn.SetActive(seasonPassAvatarInfo.IsPremium && !seasonPassAvatarInfo.IsPremiumPlus);
            premiumPlusIcon.SetActive(seasonPassAvatarInfo.IsPremiumPlus);

            // 보상 샐 데이터 갱신
            RefreshRewardCells(rewardListData, existLastCell);

            // 라인 이미지 채우는 비율 계산 (현재 레벨 - 1) / (총 셀 갯수 - 1) 래벨이 1부터 시작임을 가정.
            lineImage.fillAmount = (float)(currentLevel - 1) / (float)(rewardListData.Count - 1);

            // 현재 래밸까지 스크롤 이동 연출
            rewardCellScrollbar.value = 0;
            var cellIndex = Mathf.Max(0, currentLevel - 1);
            ShowCellEffect(cellIndex).Forget();
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

        private async UniTaskVoid ShowCellEffect(int cellIndex)
        {
            var tween = DOTween.To(() => rewardCellScrollbar.value,
                value => rewardCellScrollbar.value = value, CalculateScrollerStartPosition(cellIndex), scrollDuration).SetEase(Ease.OutQuart);
            rewardCellScrollbar.value = 0;
            tween.Play();

            for (var i = cellIndex; i < rewardCells.Count; i++)
            {
                rewardCells[i].SetTweeningStarting();
            }

            await UniTask.Delay(scrollWaitDuration);

            var durationCount = 0;
            for (var i = cellIndex; i < rewardCells.Count; i++)
            {
                rewardCells[i].ShowTweening();
                await UniTask.Delay(betweenCellViewDuration);
                durationCount += betweenCellViewDuration;
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
            ApiClients.Instance.SeasonPassServiceManager.ReceiveAll(
                (result) =>
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"), NotificationCell.NotificationType.Notification);
                    ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
                },
                (error) => { OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), NotificationCell.NotificationType.Notification); });
        }

        public void PrevSeasonClaim()
        {
            prevSeasonClaimButton.SetConditionalState(false);

            ApiClients.Instance.SeasonPassServiceManager.PrevClaim(
                result =>
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"),
                        NotificationCell.NotificationType.Notification);
                    ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
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

        public int TestCellCount = 20;
        public int TestCurrentCellLevel = 3;
        public bool TestLastCell = true;
        [ContextMenu("TestScrollRefresh")]
        public void TestScrollRefresh()
        {
            AddRewardCellIfNeeded(TestCellCount);

            for (var i = 0; i < rewardCells.Count; i++)
            {
                rewardCells[i].gameObject.SetActive(i < TestCellCount);
                rewardCells[i].SetLevelText(i + 1);
            }
            lastRewardCell.gameObject.SetActive(TestLastCell);

            scrollHorizontalLayout.CalculateLayoutInputHorizontal();
            scrollContents.sizeDelta = new Vector2(scrollHorizontalLayout.preferredWidth, scrollContents.sizeDelta.y);
            CalculateLineImageWith(TestLastCell);

            // 라인 이미지 채우는 비율 계산 (현재 레벨 - 1) / (총 셀 갯수 - 1) 래벨이 1부터 시작임을 가정.
            lineImage.fillAmount = (float)(TestCurrentCellLevel - 1) / (float)(TestCellCount - 1);

            ShowCellEffect(TestCurrentCellLevel - 1).Forget();
        }
    }
}
