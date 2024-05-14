using Cysharp.Threading.Tasks;
using Nekoyume.Action.AdventureBoss;
using Nekoyume.Blockchain;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.State;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.Threading;
using System;

namespace Nekoyume.UI
{
    using UniRx;
    public class AdventureBossRewardPopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI blockIndex;
        [SerializeField]
        private TextMeshProUGUI seasonText;
        [SerializeField]
        private TextMeshProUGUI bountyCost;
        [SerializeField]
        private TextMeshProUGUI myScore;
        [SerializeField]
        private GameObject rewardItemsBounty;
        [SerializeField]
        private GameObject rewardItemsExplore;
        [SerializeField]
        private GameObject noRewardItemsBounty;
        [SerializeField]
        private GameObject noRewardItemsExplore;

        [SerializeField]
        private ConditionalButton receiveAllButton;
        [SerializeField]
        private ConditionalButton[] pageButton;

        private long _targetBlockIndex;
        private List<SeasonInfo> _endedClaimableSeasonInfo = new List<SeasonInfo>();
        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private void Awake()
        {
            receiveAllButton.OnClickSubject.Subscribe(_ =>
            {
                foreach (var seasonInfo in _endedClaimableSeasonInfo)
                {
                    ActionManager.Instance.ClaimAdventureBossReward(seasonInfo.Season);
                    ActionManager.Instance.ClaimWantedReward(seasonInfo.Season);  
                }
                Game.Game.instance.AdventureBossData.IsRewardLoading.Value = true;
                Close();
            }).AddTo(gameObject);
        }

        private void UpdateViewAsync(long blockIndex)
        {
            RefreshBlockIndex();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;
            _endedClaimableSeasonInfo = adventureBossData.EndedSeasonInfos.Values.
                Where(seasonInfo => seasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration > Game.Game.instance.Agent.BlockIndex).
                OrderBy(seasonInfo => seasonInfo.EndBlockIndex).
                Take(pageButton.Count()).ToList();

            if (_endedClaimableSeasonInfo.Count == 0)
            {
                Close();
                return;
            }

            RefreshWithSeasonInfo(_endedClaimableSeasonInfo[0]);

            if(_endedClaimableSeasonInfo.Count == 1)
            {
                foreach (var button in pageButton)
                {
                    button.gameObject.SetActive(false);
                }
            }
            else
            {
                for(int i = 0; i < pageButton.Length; i++)
                {
                    if(i >= _endedClaimableSeasonInfo.Count)
                    {
                        pageButton[i].gameObject.SetActive(false);
                        continue;
                    }
                    pageButton[i].gameObject.SetActive(true);

                    pageButton[i].OnClickDisabledSubject.Subscribe(_ =>
                    {
                        RefreshWithSeasonInfo(_endedClaimableSeasonInfo[i]);
                        for (int z = 0; z < pageButton.Length; z++)
                        {
                            pageButton[z].Interactable = i == z;
                        }
                    }).AddTo(gameObject);
                }
            }
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesByEnable.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
        }

        private void RefreshWithSeasonInfo(SeasonInfo info)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _targetBlockIndex = info.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration;
            RefreshBlockIndex();
            seasonText.text = $"Season {info.Season}";

            bountyCost.text = "-";
            myScore.text = "-";

            rewardItemsBounty.SetActive(false);
            rewardItemsExplore.SetActive(false);
            noRewardItemsBounty.SetActive(false);
            noRewardItemsExplore.SetActive(false);

            Game.Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, info.Season).ContinueWith(task =>
            {
                if(task.IsCanceled || task.IsFaulted)
                {
                    return;
                }
                var exploreInfo = task.Result;
                if (exploreInfo != null)
                {
                    myScore.text = $"{exploreInfo.Score:#,0}";
                    SetExploreInfoVIew(true);
                }

                SetExploreInfoVIew(false);
            }, _cancellationTokenSource.Token);
            Game.Game.instance.Agent.GetBountyBoardAsync(info.Season).ContinueWith((task) =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    return;
                }
                var bountyBoard = task.Result;
                if (bountyBoard != null)
                {
                    var bountyInfo = bountyBoard.Investors.
                        Where(i => i.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address)).
                        FirstOrDefault();
                    if (bountyInfo != null)
                    {
                        bountyCost.text = $"{bountyInfo.Price.ToCurrencyNotation()}";
                        SetBountyInfoView(true);
                        return;
                    }
                }
                SetBountyInfoView(false);
            }, _cancellationTokenSource.Token);
        }

        private void SetBountyInfoView(bool visible)
        {
            rewardItemsBounty.SetActive(visible);
            noRewardItemsBounty.SetActive(!visible);
        }

        private void SetExploreInfoVIew(bool visible)
        {
            rewardItemsExplore.SetActive(visible);
            noRewardItemsExplore.SetActive(!visible);
        }

        private void RefreshBlockIndex()
        {
            var remainingBlockIndex = _targetBlockIndex - Game.Game.instance.Agent.BlockIndex;
            blockIndex.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
        }
    }
}
