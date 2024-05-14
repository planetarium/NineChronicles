using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Nekoyume.Model.AdventureBoss;
using Cysharp.Threading.Tasks;
using Nekoyume.Action.AdventureBoss;

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

        public ReactiveProperty<LatestSeason> LatestSeason = new ReactiveProperty<LatestSeason>();
        public ReactiveProperty<SeasonInfo> SeasonInfo = new ReactiveProperty<SeasonInfo>();
        public ReactiveProperty<BountyBoard> BountyBoard = new ReactiveProperty<BountyBoard>();
        public ReactiveProperty<ExploreInfo> ExploreInfo = new ReactiveProperty<ExploreInfo>();
        public ReactiveProperty<AdventureBossSeasonState> CurrentState = new ReactiveProperty<AdventureBossSeasonState>();

        public Dictionary<long,SeasonInfo> EndedSeasonInfos = new Dictionary<long, SeasonInfo>();

        private const int _endedSeasonSearchTryCount = 10;

        public void Initialize()
        {
            RefreshAllByCurrentState().Forget();
        }

        public async UniTask RefreshAllByCurrentState()
        {
            LatestSeason.Value = await Game.Game.instance.Agent.GetAdventureBossLatestSeasonAsync();
            
            //최대 10개의 종료된 시즌정보를 가져온다.(만일을 대비 해서)
            for (int i = 1; i < _endedSeasonSearchTryCount; i++)
            {
                var oldSeasonIndex = LatestSeason.Value.SeasonId - i;

                //최초시즌이 0이하로 내려가면 더이상 찾지않음.
                if (oldSeasonIndex <= 0)
                    break;

                //이미 가져온 시즌정보는 다시 가져오지않음.
                if(!EndedSeasonInfos.TryGetValue(oldSeasonIndex, out var oldSeasonInfo))
                {
                    oldSeasonInfo = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(oldSeasonIndex);
                    EndedSeasonInfos.Add(oldSeasonIndex, oldSeasonInfo);
                    //보상수령기간이 지날경우 더이상 가져오지않음.
                    if(oldSeasonInfo.EndBlockIndex + ClaimWantedReward.ClaimableDuration < Game.Game.instance.Agent.BlockIndex)
                    {
                        break;
                    }
                }
            }

            //시즌이 진행중인 경우.
            if(LatestSeason.Value.StartBlockIndex <= Game.Game.instance.Agent.BlockIndex && Game.Game.instance.Agent.BlockIndex < LatestSeason.Value.EndBlockIndex)
            {
                SeasonInfo.Value = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(LatestSeason.Value.SeasonId);
                if(SeasonInfo.Value == null)
                {
                    SeasonInfo.Value = null;
                    BountyBoard.Value = null;
                    ExploreInfo.Value = null;
                    CurrentState.Value = AdventureBossSeasonState.None;
                    NcDebug.LogError("[AdventureBossData.RefreshAllByCurrentState] SeasonInfo is null When Progress");
                    return;
                }
                BountyBoard.Value = await Game.Game.instance.Agent.GetBountyBoardAsync(SeasonInfo.Value.Season);
                if(Game.Game.instance.States.CurrentAvatarState != null)
                {
                    ExploreInfo.Value = await Game.Game.instance.Agent.GetExploreInfoAsync(Game.Game.instance.States.CurrentAvatarState.address, SeasonInfo.Value.Season);
                }
                CurrentState.Value = AdventureBossSeasonState.Progress;
                return;
            }

            SeasonInfo.Value = null;
            BountyBoard.Value = null;
            ExploreInfo.Value = null;

            //시즌시작은되었으나 아무도 현상금을 걸지않아서 시즌데이터가없는경우.
            if (LatestSeason.Value.NextStartBlockIndex <= Game.Game.instance.Agent.BlockIndex)
            {
                CurrentState.Value = AdventureBossSeasonState.Ready;
                return;
            }

            //시즌이 종료후 대기중인 경우.
            //종료된 상황인경우 시즌정보 받아서 저장.
            if (LatestSeason.Value.SeasonId > 0 && !EndedSeasonInfos.TryGetValue(LatestSeason.Value.SeasonId, out var endedSeasonInfo))
            {
                endedSeasonInfo = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(LatestSeason.Value.SeasonId);
                EndedSeasonInfos.Add(LatestSeason.Value.SeasonId, endedSeasonInfo);
            }
            CurrentState.Value = AdventureBossSeasonState.End;
            return;
        }
    }
}
