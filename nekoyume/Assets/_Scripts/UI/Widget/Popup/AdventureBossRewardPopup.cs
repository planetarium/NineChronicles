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
    using Nekoyume.Helper;
    using UniRx;
    public class AdventureBossRewardPopup : PopupWidget
    {
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
        private BaseItemView[] rewardItems;
        [SerializeField]
        private BaseItemView[] rewardItemsExplores;

        private long _targetBlockIndex;
        private List<SeasonInfo> _endedClaimableSeasonInfo = new List<SeasonInfo>();
        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected override void Awake()
        {
            receiveAllButton.OnClickSubject.Subscribe(_ =>
            {
                foreach (var seasonInfo in _endedClaimableSeasonInfo)
                {
                    ActionManager.Instance.ClaimAdventureBossReward(seasonInfo.Season);
                }
                Game.Game.instance.AdventureBossData.IsRewardLoading.Value = true;
                Close();
            }).AddTo(gameObject);
            base.Awake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;
            _endedClaimableSeasonInfo = adventureBossData.EndedSeasonInfos.Values.
                Where(seasonInfo => seasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration > Game.Game.instance.Agent.BlockIndex).
                OrderBy(seasonInfo => seasonInfo.EndBlockIndex).
                ToList();

            if (_endedClaimableSeasonInfo.Count == 0)
            {
                Close();
                return;
            }

            RefreshWithSeasonInfo(_endedClaimableSeasonInfo[0]);


            base.Show(ignoreShowAnimation);
        }

        public async UniTaskVoid LoadTotalRewards()
        {
            ClaimableReward wantedClaimableReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            ClaimableReward exprolerClaimableReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            foreach (var seasonInfo in _endedClaimableSeasonInfo)
            {
                var bountyBoard = await Game.Game.instance.Agent.GetBountyBoardAsync(seasonInfo.Season);
                var exploreBoard = await Game.Game.instance.Agent.GetExploreBoardAsync(seasonInfo.Season);
                var exploreInfo = await Game.Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, seasonInfo.Season);

                var investor = bountyBoard.Investors.FirstOrDefault(
                    inv => inv.AvatarAddress == Game.Game.instance.States.CurrentAvatarState.address);

                try
                {
                    if(investor != null && !investor.Claimed)
                    {
                        wantedClaimableReward = AdventureBossHelper.CalculateWantedReward(wantedClaimableReward, bountyBoard, Game.Game.instance.States.CurrentAvatarState.address, false, out var wantedReward);
                    }
                    if(exploreInfo != null && !exploreInfo.Claimed)
                    {
                        exprolerClaimableReward = AdventureBossHelper.CalculateExploreReward(exprolerClaimableReward, bountyBoard, exploreBoard, exploreInfo, Game.Game.instance.States.CurrentAvatarState.address, false, out var explorerReward);
                    }
                }
                catch (Exception e)
                {
                    NcDebug.LogError(e);
                }
            }

            if (wantedClaimableReward.ItemReward.Count > 0 || wantedClaimableReward.FavReward.Count > 0)
            {
                rewardItemsBounty.SetActive(true);
                noRewardItemsBounty.SetActive(false);
                int i = 0;
                foreach (var itemReward in wantedClaimableReward.ItemReward)
                {
                    rewardItems[i].ItemViewSetItemData(itemReward.Key, itemReward.Value);
                    i++;
                }
                foreach (var favReward in wantedClaimableReward.FavReward)
                {
                    rewardItems[i].ItemViewSetCurrencyData(favReward.Key, favReward.Value);
                    i++;
                }
            }
            else
            {
                rewardItemsBounty.SetActive(false);
                noRewardItemsBounty.SetActive(true);
            }

            if (exprolerClaimableReward.ItemReward.Count > 0 || exprolerClaimableReward.FavReward.Count > 0)
            {
                rewardItemsExplore.SetActive(true);
                noRewardItemsExplore.SetActive(false);
                int i = 0;
                foreach (var itemReward in exprolerClaimableReward.ItemReward)
                {
                    rewardItemsExplores[i].ItemViewSetItemData(itemReward.Key, itemReward.Value);
                    i++;
                }
                foreach (var favReward in exprolerClaimableReward.FavReward)
                {
                    rewardItemsExplores[i].ItemViewSetCurrencyData(favReward.Key, favReward.Value);
                    i++;
                }
            }
            else
            {
                rewardItemsExplore.SetActive(false);
                noRewardItemsExplore.SetActive(true);
            }

            if(noRewardItemsBounty.activeSelf && noRewardItemsExplore.activeSelf)
            {
                Close();
            }
            return;
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
    }
}
