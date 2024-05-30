using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Nekoyume.Model.AdventureBoss;
using Cysharp.Threading.Tasks;
using Nekoyume.Action.AdventureBoss;
using System.Linq;
using Codice.Client.BaseCommands.Merge;
using Libplanet.Types.Assets;
using Nekoyume.State;
using Amazon.Runtime.Internal.Transform;
using Nekoyume.Helper;

namespace Nekoyume.UI.Model
{
    public class AdventureBossData
    {
        public enum AdventureBossSeasonState
        {
            None,
            Ready,
            Progress,
            End
        }
        // FIXME: This may temporary
        public AdventureBossReward[] WantedRewardList =
        {
            new ()
            {
                BossId = 206007,
                wantedReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600201, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                    },
                    RandomRewardFavTickerDict = new Dictionary<int, int>
                    {
                        { 20001, 20 }, { 30001, 20 }
                    }
                },
                exploreReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600202, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>(),
                    RandomRewardFavTickerDict = new Dictionary<int, int>(),
                }
            },
            new ()
            {
                BossId = 208007,
                wantedReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600202, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                    },
                    RandomRewardFavTickerDict = new Dictionary<int, int>
                    {
                        { 20001, 20 }, { 30001, 20 }
                    }
                },
                exploreReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600202, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>(),
                    RandomRewardFavTickerDict = new Dictionary<int, int>(),
                }
            },
            new ()
            {
                BossId = 207007,
                wantedReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>(),
                    FixedRewardFavIdDict = new Dictionary<int, int>
                    {
                        { 20001, 50 }, { 30001, 50 }
                    },
                    RandomRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                    },
                    RandomRewardFavTickerDict = new Dictionary<int, int>
                    {
                        { 20001, 20 }, { 30001, 20 }
                    }
                },
                exploreReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600203, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>(),
                    RandomRewardFavTickerDict = new Dictionary<int, int>(),
                }
            },
            new ()
            {
                BossId = 209007,
                wantedReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600203, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600201, 20 }, { 600202, 20 }, { 600203, 20 }
                    },
                    RandomRewardFavTickerDict = new Dictionary<int, int>
                    {
                        { 20001, 20 }, { 30001, 20 }
                    }
                },
                exploreReward = new RewardInfo
                {
                    FixedRewardItemIdDict = new Dictionary<int, int>
                    {
                        { 600203, 100 }
                    },
                    FixedRewardFavIdDict = new Dictionary<int, int>(),
                    RandomRewardItemIdDict = new Dictionary<int, int>(),
                    RandomRewardFavTickerDict = new Dictionary<int, int>(),
                }
            },
        };

        public ReactiveProperty<SeasonInfo> SeasonInfo = new ReactiveProperty<SeasonInfo>();
        public ReactiveProperty<BountyBoard> BountyBoard = new ReactiveProperty<BountyBoard>();
        public ReactiveProperty<ExploreBoard> ExploreBoard = new ReactiveProperty<ExploreBoard>();
        public ReactiveProperty<Explorer> ExploreInfo = new ReactiveProperty<Explorer>();
        public ReactiveProperty<AdventureBossSeasonState> CurrentState = new ReactiveProperty<AdventureBossSeasonState>();
        public ReactiveProperty<bool> IsRewardLoading = new ReactiveProperty<bool>();

        public Dictionary<long,SeasonInfo> EndedSeasonInfos = new Dictionary<long, SeasonInfo>();
        public Dictionary<long, BountyBoard> EndedBountyBoards = new Dictionary<long, BountyBoard>();

        private const int _endedSeasonSearchTryCount = 10;


        public void Initialize()
        {
            IsRewardLoading.Value = false;
            RefreshAllByCurrentState().Forget();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe((System.Action<long>)(blockIndex =>
            {
                if(this.SeasonInfo.Value == null)
                {
                    return;
                }

                if(this.SeasonInfo.Value.EndBlockIndex == blockIndex ||
                    this.SeasonInfo.Value.NextStartBlockIndex == blockIndex)
                {
                    RefreshAllByCurrentState().Forget();
                }
            })) ;
        }

        public async UniTask RefreshAllByCurrentState()
        {
            SeasonInfo.Value = await Game.Game.instance.Agent.GetAdventureBossLatestSeasonAsync();
            
            //최대 10개의 종료된 시즌정보를 가져온다.(만일을 대비 해서)
            for (int i = 1; i < _endedSeasonSearchTryCount; i++)
            {
                var oldSeasonIndex = SeasonInfo.Value.Season - i;

                //최초시즌이 0이하로 내려가면 더이상 찾지않음.
                if (oldSeasonIndex <= 0)
                    break;

                //이미 가져온 시즌정보는 다시 가져오지않음.
                if(!EndedSeasonInfos.TryGetValue(oldSeasonIndex, out var oldSeasonInfo))
                {
                    oldSeasonInfo = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(oldSeasonIndex);
                    var oldBountyBoard = await Game.Game.instance.Agent.GetBountyBoardAsync(oldSeasonIndex);
                    EndedSeasonInfos.Add(oldSeasonIndex, oldSeasonInfo);
                    EndedBountyBoards.Add(oldSeasonIndex, oldBountyBoard);

                    //보상수령기간이 지날경우 더이상 가져오지않음.
                    if(oldSeasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration < Game.Game.instance.Agent.BlockIndex)
                    {
                        break;
                    }
                }
            }

            //시즌이 진행중인 경우.
            if(SeasonInfo.Value.StartBlockIndex <= Game.Game.instance.Agent.BlockIndex && Game.Game.instance.Agent.BlockIndex < SeasonInfo.Value.EndBlockIndex)
            {
                BountyBoard.Value = await Game.Game.instance.Agent.GetBountyBoardAsync(SeasonInfo.Value.Season);
                ExploreBoard.Value = await Game.Game.instance.Agent.GetExploreBoardAsync(SeasonInfo.Value.Season);
                if(Game.Game.instance.States.CurrentAvatarState != null)
                {
                    ExploreInfo.Value = await Game.Game.instance.Agent.GetExploreInfoAsync(Game.Game.instance.States.CurrentAvatarState.address, SeasonInfo.Value.Season);
                }
                CurrentState.Value = AdventureBossSeasonState.Progress;
                NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] Progress");
                return;
            }

            try
            {
                BountyBoard.Value = null;
                ExploreInfo.Value = null;
                ExploreBoard.Value = null;
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[AdventureBossData]{e}");
            }

            //시즌시작은되었으나 아무도 현상금을 걸지않아서 시즌데이터가없는경우.
            if (SeasonInfo.Value.NextStartBlockIndex <= Game.Game.instance.Agent.BlockIndex)
            {
                try
                {
                    CurrentState.Value = AdventureBossSeasonState.Ready;
                }
                catch (System.Exception e)
                {
                    NcDebug.LogError($"[AdventureBossData]{e}");
                }
                NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] Ready");
                return;
            }

            //시즌이 종료후 대기중인 경우.
            //종료된 상황인경우 시즌정보 받아서 저장.
            if (SeasonInfo.Value.Season > 0 && !EndedSeasonInfos.TryGetValue(SeasonInfo.Value.Season, out var endedSeasonInfo))
            {
                EndedSeasonInfos.Add(SeasonInfo.Value.Season, SeasonInfo.Value);
                EndedBountyBoards.Add(SeasonInfo.Value.Season, BountyBoard.Value);
            }
            try
            {
                CurrentState.Value = AdventureBossSeasonState.End;
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[AdventureBossData]{e}");
            }
            NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] End");
            return;
        }

        public Investor GetCurrentInvestorInfo()
        {
            if (BountyBoard.Value == null)
            {
                return null;
            }

            return BountyBoard.Value.Investors.Find(i => i.AvatarAddress == Game.Game.instance.States.CurrentAvatarState.address);
        }

        public FungibleAssetValue GetCurrentBountyPrice()
        {
            FungibleAssetValue total = new FungibleAssetValue(States.Instance.GoldBalanceState.Gold.Currency, 0, 0);
            if(BountyBoard.Value == null || BountyBoard.Value.Investors == null)
            {
                return total;
            }

            foreach (var item in BountyBoard.Value.Investors)
            {
                total += item.Price;
            }

            return total;
        }

        public ClaimableReward GetCurrentTotalRewards()
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            if(SeasonInfo.Value == null ||
                BountyBoard.Value == null ||
                ExploreBoard.Value == null ||
                ExploreInfo.Value == null)
            {
                return myReward;
            }

            try
            {
                myReward = AdventureBossHelper.CalculateExploreReward(myReward,
                                    BountyBoard.Value,
                                    ExploreBoard.Value,
                                    ExploreInfo.Value,
                                    ExploreInfo.Value.AvatarAddress,
                                    false,
                                    out var ncgReward);

                myReward = AdventureBossHelper.CalculateWantedReward(myReward,
                                    BountyBoard.Value,
                                    Game.Game.instance.States.CurrentAvatarState.address,
                                    false,
                                    out var wantedReward);
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[AdventureBossData.GetCurrentTotalRewards]{e}");
            }
            return myReward;
        }

        public ClaimableReward GetCurrentBountyRewards()
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            myReward = AdventureBossHelper.CalculateWantedReward(myReward, BountyBoard.Value, Game.Game.instance.States.CurrentAvatarState.address, false, out var wantedReward);
            return myReward;
        }
    }
}
