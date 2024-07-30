using DG.Tweening;
using Nekoyume.UI.Module;
using Nekoyume.ValueControlComponents.Shader;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Game.Controller;
    using Helper;
    using L10n;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.Mail;
    using Model;
    using System.Linq;
    using UniRx;
    using static Data.AdventureBossGameData;
    using static Scroller.NotificationCell;
    using Nekoyume.State;

    public class AdventureBoss : Widget
    {
        [SerializeField] private RectTransform towerRect;
        [SerializeField] private float towerCenterAdjuster = 52;
        [SerializeField] private Ease towerMoveEase = Ease.OutCirc;
        [SerializeField] private TextMeshProUGUI clearFloor;
        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private TextMeshProUGUI totalBounty;
        [SerializeField] private TextMeshProUGUI participantsCount;
        [SerializeField] private TextMeshProUGUI usedApPotion;
        [SerializeField] private TextMeshProUGUI remainingBlockTime;
        [SerializeField] private ShaderPropertySlider remainingBlockTimeSlider;

        [SerializeField] private ConditionalButton addBountyButton;
        [SerializeField] private ConditionalButton viewAllButton;
        [SerializeField] private ConditionalButton enterButton;

        [SerializeField] private TextMeshProUGUI[] investorUserNames;
        [SerializeField] private TextMeshProUGUI[] investorBountyCounts;
        [SerializeField] private TextMeshProUGUI[] investorBountyPrice;

        [SerializeField] private TextMeshProUGUI myUserNames;
        [SerializeField] private TextMeshProUGUI myBountyCounts;
        [SerializeField] private TextMeshProUGUI myBountyPrice;
        [SerializeField] private AdventureBossFloor[] floors;
        [SerializeField] private BaseItemView[] baseItemViews;
        [SerializeField] private TextMeshProUGUI bossName;
        [SerializeField] private Transform bossImageParent;
        [SerializeField] private TextMeshProUGUI randomRewardText;
        [SerializeField] private TextMeshProUGUI rewardRemainTimeText;

        public AdventureBossFloor CurrentUnlockFloor;

        private int _bossId;
        private GameObject _bossImage;

        private const float _floorHeight = 170;
        private readonly List<IDisposable> _disposablesByEnable = new();
        private long _seasonStartBlock;
        private long _seasonEndBlock;

        private ClaimableReward _myReward = new()
        {
            NcgReward = null,
            ItemReward = new Dictionary<int, int>(),
            FavReward = new Dictionary<int, int>()
        };

        protected override void Awake()
        {
            base.Awake();
            addBountyButton.OnClickSubject.ThrottleFirst(TimeSpan.FromSeconds(1)).Subscribe(_ => { Find<AdventureBossEnterBountyPopup>().Show(); }).AddTo(gameObject);

            addBountyButton.OnClickDisabledSubject.ThrottleFirst(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_ADVENTURE_BOSS_CAN_NOT_BOUNTY"),
                    NotificationType.Information);
            }).AddTo(gameObject);

            viewAllButton.OnClickSubject.Subscribe(_ => { Find<AdventureBossFullBountyStatusPopup>().Show(); }).AddTo(gameObject);
            viewAllButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_VIEW_ALL"));

            enterButton.OnClickSubject.Subscribe(_ => { Find<AdventureBossBattlePopup>().Show(); })
                .AddTo(gameObject);
            enterButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_ENTER"));
            remainingBlockTime.text = "(-)";
        }

        private void SetBossData(int bossId)
        {
            if (_bossId != bossId)
            {
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
                }

                _bossId = bossId;
                _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconBody(_bossId),
                    bossImageParent);
                _bossImage.transform.localPosition = Vector3.zero;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            if (Game.Game.instance.AdventureBossData.CurrentState.Value !=
                AdventureBossData.AdventureBossSeasonState.Progress)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"),
                    NotificationType.Alert);
                NcDebug.LogError("[UI_AdventureBoss] Show: Invalid state");
                return;
            }

            towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);

            try
            {
                Game.Game.instance.AdventureBossData.SeasonInfo.Subscribe(RefreshSeasonInfo)
                    .AddTo(_disposablesByEnable);

                Game.Game.instance.AdventureBossData.BountyBoard.Subscribe(RefreshBountyBoardInfo)
                    .AddTo(_disposablesByEnable);

                Game.Game.instance.AdventureBossData.ExploreInfo.Subscribe(RefreshExploreInfo)
                    .AddTo(_disposablesByEnable);

                Game.Game.instance.AdventureBossData.ExploreBoard.Subscribe(RefreshExploreBoard)
                    .AddTo(_disposablesByEnable);

                Game.Game.instance.Agent.BlockIndexSubject
                    .StartWith(Game.Game.instance.Agent.BlockIndex)
                    .Subscribe(UpdateViewAsync)
                    .AddTo(_disposablesByEnable);

                Find<HeaderMenuStatic>()
                    .Show(HeaderMenuStatic.AssetVisibleState.AdventureBoss);

                AudioController.instance.PlayMusic(AudioController.MusicCode.AdventureBossLobby);
                base.Show(ignoreShowAnimation);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"),
                    NotificationType.Alert);
                throw;
            }
        }

        private void RefreshExploreBoard(ExploreBoard board)
        {
            try
            {
                if (board == null)
                {
                    participantsCount.text = "0";
                    return;
                }

                participantsCount.text = $"{board.ExplorerCount:#,0}";
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void RefreshExploreInfo(Explorer exploreInfo)
        {
            CurrentUnlockFloor = null;
            var usedMyApPotion = 0;
            var adventureBossData = Game.Game.instance.AdventureBossData;
            if (exploreInfo == null)
            {
                clearFloor.text = $"-";
                score.text = "0";
                if (adventureBossData.ExploreBoard.Value != null)
                {
                    usedApPotion.text = $"{adventureBossData.ExploreBoard.Value.UsedApPotion:#,0} (0)";
                }
                ChangeFloor(1);
                for (var i = 0; i < floors.Count(); i++)
                {
                    if (i < 5)
                    {
                        floors[i].SetState(AdventureBossFloor.FloorState.NotClear, i);
                    }
                    else if (i == 5 && Game.Game.instance.AdventureBossData.GetCurrentUnlockFloorCost(i + 1, out var unlockCostData))
                    {
                        floors[i].SetState(AdventureBossFloor.FloorState.UnLock, i, unlockCostData);
                        if (CurrentUnlockFloor == null)
                        {
                            CurrentUnlockFloor = floors[i];
                        }
                    }
                    else
                    {
                        floors[i].SetState(AdventureBossFloor.FloorState.Lock, i);
                    }
                }

                return;
            }

            usedMyApPotion = exploreInfo.UsedApPotion;
            clearFloor.text = $"{exploreInfo.Floor}F";

            for (var i = 0; i < floors.Count(); i++)
            {
                if (i < exploreInfo.Floor)
                {
                    floors[i].SetState(AdventureBossFloor.FloorState.Clear, i);
                }
                else if (i > exploreInfo.MaxFloor)
                {
                    floors[i].SetState(AdventureBossFloor.FloorState.Lock, i);
                }
                else if (i >= exploreInfo.MaxFloor &&
                    Game.Game.instance.AdventureBossData.GetCurrentUnlockFloorCost(i + 1, out var unlockData))
                {
                    floors[i].SetState(AdventureBossFloor.FloorState.UnLock, i, unlockData);
                    if (CurrentUnlockFloor == null)
                    {
                        CurrentUnlockFloor = floors[i];
                    }
                }
                else
                {
                    floors[i].SetState(AdventureBossFloor.FloorState.NotClear, i);
                }
            }
            double contribution = 0;
            if (adventureBossData.ExploreBoard.Value != null && adventureBossData.ExploreBoard.Value.TotalPoint != 0)
            {
                contribution = exploreInfo.Score == 0 ? 0 : (double)exploreInfo.Score / adventureBossData.ExploreBoard.Value.TotalPoint * 100;
            }
            score.text = $"{exploreInfo.Score:#,0} ({contribution.ToString("F2")}%)";

            usedApPotion.text = "0";
            if(adventureBossData.ExploreBoard.Value != null)
            {
                usedApPotion.text = $"{adventureBossData.ExploreBoard.Value.UsedApPotion:#,0} ({usedMyApPotion})";
            }

            ChangeFloor(Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor + 1, false);
            RefreshMyReward();
        }

        private void RefreshMyReward()
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;
            _myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>()
            };

            try
            {
                _myReward = AdventureBossData.AddClaimableReward(_myReward,
                    adventureBossData.GetCurrentExploreRewards());
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }

            try
            {
                _myReward = AdventureBossData.AddClaimableReward(_myReward,
                    adventureBossData.GetCurrentBountyRewards());
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }

            var itemViewIndex = 0;
            if (_myReward.NcgReward != null && _myReward.NcgReward.HasValue && !_myReward.NcgReward.Value.RawValue.IsZero)
            {
                baseItemViews[itemViewIndex].ItemViewSetCurrencyData(_myReward.NcgReward.Value);
                itemViewIndex++;
            }

            foreach (var item in _myReward.ItemReward)
            {
                if (itemViewIndex >= baseItemViews.Length)
                {
                    break;
                }

                baseItemViews[itemViewIndex].ItemViewSetItemData(item.Key, item.Value);
                itemViewIndex++;
            }

            foreach (var fav in _myReward.FavReward)
            {
                if (itemViewIndex >= baseItemViews.Length)
                {
                    break;
                }

                if (baseItemViews[itemViewIndex].ItemViewSetCurrencyData(fav.Key, fav.Value))
                {
                    itemViewIndex++;
                }
            }

            for (; itemViewIndex < baseItemViews.Length; itemViewIndex++)
            {
                baseItemViews[itemViewIndex].gameObject.SetActive(false);
            }
        }

        private void RefreshBountyBoardInfo(BountyBoard board)
        {
            totalBounty.text =
                $"{Game.Game.instance.AdventureBossData.GetCurrentBountyPrice().MajorUnit.ToString("#,0")}";
            var investInfo = Game.Game.instance.AdventureBossData.GetCurrentInvestorInfo();
            if (investInfo == null)
            {
                addBountyButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_ADD", 0,
                    Investor.MaxInvestmentCount));
                addBountyButton.Interactable = true;

                myUserNames.text = Game.Game.instance.States.CurrentAvatarState.name;
                myBountyCounts.text = $"(0/{Investor.MaxInvestmentCount})";
                myBountyPrice.text = "-";
            }
            else
            {
                addBountyButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_ADD",
                    investInfo.Count, Investor.MaxInvestmentCount));
                if (investInfo.Count < Investor.MaxInvestmentCount)
                {
                    addBountyButton.Interactable = true;
                }
                else
                {
                    addBountyButton.Interactable = false;
                }

                myUserNames.text = Game.Game.instance.States.CurrentAvatarState.name;
                myBountyCounts.text = $"({investInfo.Count}/{Investor.MaxInvestmentCount})";
                myBountyPrice.text = investInfo.Price.MajorUnit.ToString("#,0");
            }

            if (board == null || board.Investors == null)
            {
                for (var i = 0; i < 3; i++)
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(false);
                }

                return;
            }

            var topInvestorList = board.Investors.OrderByDescending(investor => investor.Price)
                .Take(3).ToList();
            for (var i = 0; i < 3; i++)
            {
                if (topInvestorList.Count() > i)
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(true);
                    investorUserNames[i].text = topInvestorList[i].Name;
                    investorBountyCounts[i].text =
                        $"({topInvestorList[i].Count}/{Investor.MaxInvestmentCount})";
                    investorBountyPrice[i].text =
                        topInvestorList[i].Price.MajorUnit.ToString("#,0");
                }
                else
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(false);
                }
            }

            var raffleReward = AdventureBossHelper.CalculateRaffleReward(board);
            randomRewardText.text = raffleReward.MajorUnit.ToString("#,0");

            RefreshMyReward();
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            _myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>()
            };
            if (seasonInfo == null)
            {
                NcDebug.LogError("[UI_AdventureBoss] RefreshSeasonInfo: seasonInfo is null");
                return;
            }

            _seasonStartBlock = seasonInfo.StartBlockIndex;
            _seasonEndBlock = seasonInfo.EndBlockIndex;
            var claimInterval = States.Instance.GameConfigState.AdventureBossClaimInterval;
            rewardRemainTimeText.text = L10n.L10nManager.Localize("UI_ADVENTURE_BOSS_REWARDS_REMAIN_TIME", claimInterval, claimInterval.BlockRangeToTimeSpanString());
            try
            {
                SetBossData(seasonInfo.BossId);
                bossName.text = L10nManager.LocalizeCharacterName(seasonInfo.BossId);
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var remainingBlockIndex = _seasonEndBlock - blockIndex;
            if (remainingBlockIndex < 0)
            {
                Close();
                Find<AdventureBossRewardPopup>().Show();
                return;
            }

            remainingBlockTime.text =
                $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
            remainingBlockTimeSlider.NormalizedValue =
                Mathf.InverseLerp(_seasonStartBlock, _seasonEndBlock, blockIndex);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _disposablesByEnable.DisposeAllAndClear();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Main);
        }

        public void SetBountyLoadingIndicator(bool isActive)
        {
            if (isActive)
            {
                addBountyButton.SetState(ConditionalButton.State.Conditional);
            }
            else
            {
                addBountyButton.SetState(ConditionalButton.State.Normal);
                var info = Game.Game.instance.AdventureBossData.GetCurrentInvestorInfo();
                if (info == null)
                {
                    addBountyButton.Interactable = false;
                    return;
                }

                if (info.Count < Investor.MaxInvestmentCount)
                {
                    addBountyButton.Interactable = true;
                }
                else
                {
                    addBountyButton.Interactable = false;
                }
            }
        }

        public void ChangeFloor(int targetIndex, bool isStartPointRefresh = true,
            bool isAnimation = true)
        {
            var targetCenter = targetIndex * _floorHeight + _floorHeight / 2;
            var startY = -(targetCenter - MainCanvas.instance.RectTransform.rect.height / 2 -
                towerCenterAdjuster);

            if (isAnimation)
            {
                if (isStartPointRefresh)
                {
                    towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);
                }

                towerRect.DoAnchoredMoveY(Math.Min(startY, 0), 0.35f).SetEase(towerMoveEase);
            }
            else
            {
                towerRect.anchoredPosition =
                    new Vector2(towerRect.anchoredPosition.x, Math.Min(startY, 0));
            }
        }

        public void OnClickShowRewardInfo()
        {
            Find<AdventureBossRewardInfoPopup>().Show();
        }

        public void OnClickShowPrevRewardInfo()
        {
            Find<PreviousSeasonReportPopup>().Show(Math.Max(0, Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season - 1)).Forget();
        }

        public void OnClickBossParticipantBonusPopup()
        {
            Find<AdventureBossParticipantBonusPopup>().Show();
        }

        public void OnClickFullBountyStatusPopup()
        {
            Find<AdventureBossFullBountyStatusPopup>().Show();
        }
    }
}
