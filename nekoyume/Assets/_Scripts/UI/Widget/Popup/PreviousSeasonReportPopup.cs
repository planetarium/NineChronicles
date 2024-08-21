using Cysharp.Threading.Tasks;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static Nekoyume.Data.AdventureBossGameData;

namespace Nekoyume
{
    public class PreviousSeasonReportPopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI totalBounty;
        [SerializeField]
        private TextMeshProUGUI myBounty;
        [SerializeField]
        private TextMeshProUGUI[] operationalRankUserNames;
        [SerializeField]
        private TextMeshProUGUI[] operationalRankBounties;
        [SerializeField]
        private TextMeshProUGUI randomWinner;
        [SerializeField]
        private TextMeshProUGUI randomWinnerBounty;
        [SerializeField]
        private BaseItemView[] myBountyRewards;
        [SerializeField]
        private TextMeshProUGUI totalScore;
        [SerializeField]
        private TextMeshProUGUI totalExplorer;
        [SerializeField]
        private TextMeshProUGUI totalExplorerApUsage;
        [SerializeField]
        private TextMeshProUGUI myScore;
        [SerializeField]
        private TextMeshProUGUI myApUsage;
        [SerializeField]
        private BaseItemView[] myExploreRewards;

        private static bool IsPreviousSeasonReportPopup = false;
        public async UniTaskVoid Show(long seasonIndex, bool ignoreShowAnimation = false)
        {
            if (IsPreviousSeasonReportPopup)
            {
                NcDebug.LogError("PreviousSeasonReportPopup is already opened");
                return;
            }
            IsPreviousSeasonReportPopup = true;
            try
            {
                //UI 정보 초기화
                foreach (var item in this.myBountyRewards)
                {
                    item.gameObject.SetActive(false);
                }
                foreach (var item in myExploreRewards)
                {
                    item.gameObject.SetActive(false);
                }
                totalBounty.text = "-";
                myBounty.text = "-";
                foreach (var item in operationalRankUserNames)
                {
                    item.text = "-";
                }
                foreach (var item in operationalRankBounties)
                {
                    item.text = "-";
                }
                randomWinner.text = "???";
                randomWinnerBounty.text = "-";
                totalScore.text = "-";
                totalExplorer.text = "-";
                totalExplorerApUsage.text = "-";
                myScore.text = "-";
                myApUsage.text = "-";

                if(!Game.Game.instance.AdventureBossData.EndedBountyBoards.TryGetValue(seasonIndex, out var prevBountyBoard))
                {
                    prevBountyBoard = await Game.Game.instance.Agent.GetBountyBoardAsync(seasonIndex);
                }

                if(!Game.Game.instance.AdventureBossData.EndedExploreBoards.TryGetValue(seasonIndex, out var prevExploreBoard) && string.IsNullOrEmpty(prevExploreBoard.RaffleWinnerName))
                {
                    prevExploreBoard = await Game.Game.instance.Agent.GetExploreBoardAsync(seasonIndex);
                }

                if(!Game.Game.instance.AdventureBossData.EndedExploreInfos.TryGetValue(seasonIndex, out var prevExploreInfo))
                {
                    prevExploreInfo = await Game.Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, seasonIndex);
                }

                var myBountyRewardsData = new ClaimableReward
                {
                    NcgReward = null,
                    ItemReward = new Dictionary<int, int>(),
                    FavReward = new Dictionary<int, int>()
                };
                var myExplorerRewardsData = new ClaimableReward
                {
                    NcgReward = null,
                    ItemReward = new Dictionary<int, int>(),
                    FavReward = new Dictionary<int, int>()
                };

                if (prevBountyBoard != null)
                {
                    try
                    {
                        var myInvestment = prevBountyBoard.Investors.FirstOrDefault(inv => inv.AvatarAddress == Game.Game.instance.States.CurrentAvatarState.address);
                        if(myInvestment != null)
                        {
                            myBountyRewardsData = AdventureBossHelper.CalculateWantedReward(myBountyRewardsData,
                                prevBountyBoard,
                                Game.Game.instance.States.CurrentAvatarState.address,
                                TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                out var wantedReward);
                        }
                    }
                    catch(System.Exception e)
                    {
                        NcDebug.LogError(e);
                    }
                    totalBounty.text = prevBountyBoard.totalBounty().MajorUnit.ToString("#,0");
                    var investor = prevBountyBoard.Investors.FirstOrDefault(inv => inv.AvatarAddress == States.Instance.CurrentAvatarState.address);
                    if (investor != null)
                    {
                        myBounty.text = investor.Price.MajorUnit.ToString("#,0");
                    }

                    int operationalIndex = 0;
                    prevBountyBoard.Investors.OrderByDescending(inv => inv.Price.MajorUnit).Take(3).ToList().ForEach((inv) =>
                    {
                        operationalRankUserNames[operationalIndex].text = inv.Name;
                        operationalRankBounties[operationalIndex].text = inv.Price.MajorUnit.ToString("#,0");
                        operationalIndex++;
                    });
                }
                if (prevExploreBoard != null && prevExploreInfo != null && prevExploreInfo.Score > 0)
                {
                    try
                    {
                        myExplorerRewardsData = AdventureBossHelper.CalculateExploreReward(myExplorerRewardsData,
                        prevBountyBoard,
                        prevExploreBoard,
                        prevExploreInfo,
                        prevExploreInfo.AvatarAddress,
                        TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                        States.Instance.GameConfigState.AdventureBossNcgApRatio,
                        States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                        false,
                        out var ncgReward);
                    }
                    catch(System.Exception e)
                    {
                        NcDebug.LogError(e);
                    }
                }
                long totalScoreValue = 0;
                if (prevExploreBoard != null)
                {
                    if (!string.IsNullOrEmpty(prevExploreBoard.RaffleWinnerName))
                    {
                        randomWinner.text = prevExploreBoard.RaffleWinnerName;
                    }
                    randomWinnerBounty.text = prevExploreBoard.RaffleReward == null ? "-" : prevExploreBoard.RaffleReward?.MajorUnit.ToString("#,0");
                    totalScore.text = prevExploreBoard.TotalPoint.ToString("#,0");
                    totalScoreValue = prevExploreBoard.TotalPoint;
                    totalExplorer.text = prevExploreBoard.ExplorerCount.ToString("#,0");
                    totalExplorerApUsage.text = prevExploreBoard.UsedApPotion.ToString("#,0");
                }
                if (prevExploreInfo != null)
                {
                    double contribution = 0;
                    contribution = totalScoreValue == 0 || prevExploreInfo.Score == 0 ? 0 : (double)prevExploreInfo.Score / totalScoreValue * 100;
                    myScore.text = $"{prevExploreInfo.Score.ToString("#,0")} ({contribution.ToString("F2")}%)";
                    myApUsage.text = prevExploreInfo.UsedApPotion.ToString("#,0");
                }
                RefreshRewardItemView(myBountyRewardsData, myBountyRewards);
                RefreshRewardItemView(myExplorerRewardsData, myExploreRewards);
                base.Show(ignoreShowAnimation);
                IsPreviousSeasonReportPopup = false;
            }
            catch(System.Exception e)
            {
                NcDebug.LogError(e);
                IsPreviousSeasonReportPopup = false;
            }
        }

        private void RefreshRewardItemView(ClaimableReward rewards, BaseItemView[] itemViews)
        {
            int index = 0;

            if (rewards.NcgReward != null && rewards.NcgReward.HasValue && !rewards.NcgReward.Value.RawValue.IsZero)
            {
                itemViews[index].ItemViewSetCurrencyData(rewards.NcgReward.Value);
                index++;
            }

            foreach (var item in rewards.ItemReward)
            {
                if (index >= itemViews.Length)
                {
                    NcDebug.LogError("itemViews is not enough");
                    break;
                }
                itemViews[index].ItemViewSetItemData(item.Key, item.Value);
                index++;
            }

            foreach (var fav in rewards.FavReward)
            {
                if (index >= itemViews.Length)
                {
                    NcDebug.LogError("itemViews is not enough");
                    break;
                }

                if (itemViews[index].ItemViewSetCurrencyData(fav.Key, fav.Value))
                {
                    index++;
                }
            }

            for (; index < itemViews.Length; index++)
            {
                itemViews[index].gameObject.SetActive(false);
            }
        }
    }
}
