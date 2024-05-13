using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Nekoyume.Model.AdventureBoss;
using Cysharp.Threading.Tasks;

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

        public void Initialize()
        {
            RefreshAllByCurrentState().Forget();
        }

        public async UniTask RefreshAllByCurrentState()
        {
            LatestSeason.Value = await Game.Game.instance.Agent.GetAdventureBossLatestSeasonAsync();

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
                ExploreInfo.Value = await Game.Game.instance.Agent.GetExploreInfoAsync(Game.Game.instance.States.CurrentAvatarState.address, SeasonInfo.Value.Season);
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
            CurrentState.Value = AdventureBossSeasonState.End;
            return;
        }
    }
}
