using Codice.Utils;
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
    using Nekoyume.Blockchain;
    using Nekoyume.L10n;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.Mail;
    using System.Linq;
    using UniRx;
    public class AdventureBoss : Widget
    {
        [SerializeField]
        private RectTransform towerRect;
        [SerializeField]
        private float towerCenterAdjuster = 52;
        [SerializeField]
        private Ease towerMoveEase = Ease.OutCirc;
        [SerializeField]
        private TextMeshProUGUI clearFloor;
        [SerializeField]
        private TextMeshProUGUI score;
        [SerializeField]
        private TextMeshProUGUI totalBounty;
        [SerializeField]
        private TextMeshProUGUI participantsCount;
        [SerializeField]
        private TextMeshProUGUI remainingBlockTime;
        [SerializeField]
        private ShaderPropertySlider remainingBlockTimeSlider;

        [SerializeField]
        private ConditionalButton addBountyButton;
        [SerializeField]
        private ConditionalButton challengeButton;
        [SerializeField]
        private ConditionalButton entireButton;

        [SerializeField]
        private TextMeshProUGUI[] investorUserNames;
        [SerializeField]
        private TextMeshProUGUI[] investorBountyCounts;
        [SerializeField]
        private TextMeshProUGUI[] investorBountyPrice;

        [SerializeField]
        private TextMeshProUGUI myUserNames;
        [SerializeField]
        private TextMeshProUGUI myBountyCounts;
        [SerializeField]
        private TextMeshProUGUI myBountyPrice;

        private const float _floorHeight = 170;
        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private long _seasonStartBlock;
        private long _seasonEndBlock;

        protected override void Awake()
        {
            base.Awake();
            addBountyButton.OnClickSubject.Subscribe(_ => {
                Widget.Find<AdventureBossEnterBountyPopup>().Show();
            }).AddTo(gameObject);

            //todo
            challengeButton.OnClickSubject.Subscribe(_ =>
            {
                AdventureBossBattleAction();
            }).AddTo(gameObject);
            entireButton.OnClickSubject.Subscribe(_ =>
            {
                AdventureBossBattleAction();
            }).AddTo(gameObject);
        }

        private void AdventureBossBattleAction()
        {
            Widget.Find<LoadingScreen>().Show();
            try
            {
                ActionManager.Instance.AdventureBossBattle().Subscribe(eval =>
                {
                    Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().ContinueWith(() =>
                    {
                        Widget.Find<LoadingScreen>().Close();
                    });
                });
            }
            catch (Exception e)
            {
                Widget.Find<LoadingScreen>().Close();
                NcDebug.LogError(e);
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Game.Game.instance.AdventureBossData.CurrentState.Value != Model.AdventureBossData.AdventureBossSeasonState.Progress)
            {
                OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_INVALID"), Scroller.NotificationCell.NotificationType.Alert);
                NcDebug.LogError("[UI_AdventureBoss] Show: Invalid state");
                return;
            }

            towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);

            Game.Game.instance.AdventureBossData.SeasonInfo.
                Subscribe(RefreshSeasonInfo).
                AddTo(_disposablesByEnable);

            Game.Game.instance.AdventureBossData.BountyBoard.
                Subscribe(RefreshBountyBoardInfo).
                AddTo(_disposablesByEnable);
            Game.Game.instance.AdventureBossData.ExploreInfo.
                Subscribe(RefreshExploreInfo).
                AddTo(_disposablesByEnable);

            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);

            base.Show(ignoreShowAnimation);
        }

        private void RefreshExploreInfo(ExploreInfo exploreInfo)
        {
            if(exploreInfo == null)
            {
                clearFloor.text = $"-";
                score.text = "0";
                ChangeFloor(1);
                return;
            }
            clearFloor.text = $"F{exploreInfo.Floor}";
            score.text = $"{exploreInfo.Score:#,0}";
            ChangeFloor(Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor, false);
        }

        private void RefreshBountyBoardInfo(BountyBoard board)
        {
            totalBounty.text = $"{Game.Game.instance.AdventureBossData.GetCurrentBountyPrice().MajorUnit.ToString("#,0")}";
            var investInfo = Game.Game.instance.AdventureBossData.GetCurrentInvestorInfo();
            if(investInfo == null)
            {
                addBountyButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_ADD", 0, Investor.MaxInvestmentCount));
                addBountyButton.Interactable = true;

                myUserNames.text = Game.Game.instance.States.CurrentAvatarState.name;
                myBountyCounts.text = $"(0/{Investor.MaxInvestmentCount})";
                myBountyPrice.text = "-";
            }
            else
            {
                addBountyButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_ADD", investInfo.Count, Investor.MaxInvestmentCount));
                if(investInfo.Count < Investor.MaxInvestmentCount)
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
            if(board == null || board.Investors == null)
            {
                for (int i = 0; i < 3; i++)
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(false);
                }
                return;
            }
            var topInvestorList =  board.Investors.OrderByDescending(investor => investor.Price).Take(3).ToList();
            for (int i = 0; i < 3; i++)
            {
                if(topInvestorList.Count() > i)
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(true);
                    investorUserNames[i].text = $"#{topInvestorList[i].AvatarAddress.ToHex()[..4]}";
                    investorBountyCounts[i].text = $"({topInvestorList[i].Count}/{Investor.MaxInvestmentCount})";
                    investorBountyPrice[i].text = topInvestorList[i].Price.MajorUnit.ToString("#,0");
                }
                else
                {
                    investorUserNames[i].transform.parent.parent.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            if(seasonInfo == null)
            {
                participantsCount.text = "-";
                return;
            }
            _seasonStartBlock = seasonInfo.StartBlockIndex;
            _seasonEndBlock = seasonInfo.EndBlockIndex;
            participantsCount.text = $"{seasonInfo.ExplorerList.Count:#,0}";
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var remainingBlockIndex = _seasonEndBlock - blockIndex;
            if(remainingBlockIndex < 0)
            {
                Close();
                return;
            }
            remainingBlockTime.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
            remainingBlockTimeSlider.NormalizedValue = Mathf.InverseLerp(_seasonStartBlock, _seasonEndBlock, blockIndex);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _disposablesByEnable.DisposeAllAndClear();
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
                if(info == null)
                {
                    addBountyButton.Interactable = false;
                    return;
                }

                if(info.Count < Investor.MaxInvestmentCount)
                {
                    addBountyButton.Interactable = true;
                }
                else
                {
                    addBountyButton.Interactable = false;
                }
            }
        }

        public void ChangeFloor(int targetIndex, bool isStartPointRefresh = true, bool isAnimation = true)
        {
            float targetCenter = targetIndex * _floorHeight + (_floorHeight / 2);
            float startY = -(targetCenter - (MainCanvas.instance.RectTransform.rect.height/2) - towerCenterAdjuster);

            if(isAnimation)
            {
                if (isStartPointRefresh)
                {
                    towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, 0);
                }
                towerRect.DoAnchoredMoveY(Math.Min(startY, 0), 0.35f).SetEase(towerMoveEase);
            }
            else
            {
                towerRect.anchoredPosition = new Vector2(towerRect.anchoredPosition.x, Math.Min(startY,0));
            }
        }

    }
}
