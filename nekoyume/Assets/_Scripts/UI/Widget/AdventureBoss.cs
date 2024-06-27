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
    using Nekoyume.Game.Controller;
    using Nekoyume.Helper;
    using Nekoyume.L10n;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.Mail;
    using Nekoyume.UI.Model;
    using System.Linq;
    using UniRx;
    using static Nekoyume.Data.AdventureBossGameData;
    using static Nekoyume.UI.Scroller.NotificationCell;

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

        public AdventureBossFloor CurrentUnlockFloor;

        private int _bossId;
        private GameObject _bossImage;

        private const float _floorHeight = 170;
        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private long _seasonStartBlock;
        private long _seasonEndBlock;

        private ClaimableReward _myReward = new ClaimableReward
        {
            NcgReward = null,
            ItemReward = new Dictionary<int, int>(),
            FavReward = new Dictionary<int, int>(),
        };

        protected override void Awake()
        {
            base.Awake();
            addBountyButton.OnClickSubject.ThrottleFirst(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                Widget.Find<AdventureBossEnterBountyPopup>().Show();
            }).AddTo(gameObject);

            addBountyButton.OnClickDisabledSubject.ThrottleFirst(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_ADVENTURE_BOSS_CAN_NOT_BOUNTY"),
                        NotificationType.Information);
            }).AddTo(gameObject);

            viewAllButton.OnClickSubject.Subscribe(_ =>
            {
                Widget.Find<AdventureBossFullBountyStatusPopup>().Show();
            }).AddTo(gameObject);
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
            if (Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            if (Game.Game.instance.AdventureBossData.CurrentState.Value !=
                Model.AdventureBossData.AdventureBossSeasonState.Progress)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"),
                    Scroller.NotificationCell.NotificationType.Alert);
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
                    .Subscribe(UpdateViewAsync)
                    .AddTo(_disposablesByEnable);

                Widget.Find<HeaderMenuStatic>()
                    .Show(HeaderMenuStatic.AssetVisibleState.AdventureBoss);

                AudioController.instance.PlayMusic(AudioController.MusicCode.AdventureBossLobby);
                base.Show(ignoreShowAnimation);
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"),
                    Scroller.NotificationCell.NotificationType.Alert);
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
                    usedApPotion.text = "0";
                    return;
                }

                participantsCount.text = $"{board.ExplorerCount:#,0}";
                usedApPotion.text = $"{board.UsedApPotion:#,0}";
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void RefreshExploreInfo(Explorer exploreInfo)
        {
            CurrentUnlockFloor = null;
            if (exploreInfo == null)
            {
                clearFloor.text = $"-";
                score.text = "0";
                ChangeFloor(1);
                for (int i = 0; i < floors.Count(); i++)
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

            clearFloor.text = $"{exploreInfo.Floor}F";

            for (int i = 0; i < floors.Count(); i++)
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

            score.text = $"{exploreInfo.Score:#,0}";
            ChangeFloor(Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor + 1, false);

            var adventureBossData = Game.Game.instance.AdventureBossData;
            try
            {
                _myReward = AdventureBossData.AddClaimableReward(_myReward,
                    adventureBossData.GetCurrentExploreRewards());
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }

            RefreshMyReward();
        }

        private void RefreshMyReward()
        {
            int itemViewIndex = 0;
            if (_myReward.NcgReward != null && _myReward.NcgReward.HasValue && _myReward.NcgReward.Value.MajorUnit > 0)
            {
                baseItemViews[itemViewIndex].ItemViewSetCurrencyData(
                    _myReward.NcgReward.Value.Currency.Ticker,
                    (decimal)_myReward.NcgReward.Value.MajorUnit);
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
                for (int i = 0; i < 3; i++)
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(false);
                }

                return;
            }

            var topInvestorList = board.Investors.OrderByDescending(investor => investor.Price)
                .Take(3).ToList();
            for (int i = 0; i < 3; i++)
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

            try
            {
                _myReward = AdventureBossData.AddClaimableReward(_myReward,
                    Game.Game.instance.AdventureBossData.GetCurrentBountyRewards());
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }
            
            RefreshMyReward();
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            _myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            if (seasonInfo == null)
            {
                NcDebug.LogError("[UI_AdventureBoss] RefreshSeasonInfo: seasonInfo is null");
                return;
            }

            _seasonStartBlock = seasonInfo.StartBlockIndex;
            _seasonEndBlock = seasonInfo.EndBlockIndex;
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
                Widget.Find<AdventureBossRewardPopup>().Show();
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
                Investor info = Game.Game.instance.AdventureBossData.GetCurrentInvestorInfo();
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
            float targetCenter = targetIndex * _floorHeight + (_floorHeight / 2);
            float startY = -(targetCenter - (MainCanvas.instance.RectTransform.rect.height / 2) -
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
