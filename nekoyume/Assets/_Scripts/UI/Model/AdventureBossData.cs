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
        public ReactiveProperty<LatestSeason> LatestSeason = new ReactiveProperty<LatestSeason>();
        public ReactiveProperty<SeasonInfo> SeasonInfo = new ReactiveProperty<SeasonInfo>();
        public ReactiveProperty<BountyBoard> BountyBoard = new ReactiveProperty<BountyBoard>();
        public ReactiveProperty<ExploreInfo> ExploreInfo = new ReactiveProperty<ExploreInfo>();

        public void Initialize()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(_ =>
            {
                if (SeasonInfo.Value == null || SeasonInfo.Value.EndBlockIndex < Game.Game.instance.Agent.BlockIndex)
                {
                    RefreshSeasonInfo();
                }
            });
        }

        public async UniTask RefreshAllByCurrentState()
        {
            LatestSeason.Value = await Game.Game.instance.Agent.GetAdventureBossLatestSeasonAsync();
            SeasonInfo.Value = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(LatestSeason.Value.SeasonId);
            BountyBoard.Value = await Game.Game.instance.Agent.GetBountyBoardAsync(SeasonInfo.Value.Season);
            ExploreInfo.Value = await Game.Game.instance.Agent.GetExploreInfoAsync(Game.Game.instance.States.CurrentAvatarState.address, SeasonInfo.Value.Season);
        }

        public void RefreshSeasonInfo()
        {
            Game.Game.instance.Agent.GetAdventureBossLatestSeasonInfoAsync().AsUniTask().ContinueWith(
                latestSeason =>
                {
                    SeasonInfo.Value = latestSeason;
                    RefreshBountyBoard();
                });
        }

        public void RefreshBountyBoard()
        {
            if(SeasonInfo.Value == null)
            {
                return;
            }
            Game.Game.instance.Agent.GetBountyBoardAsync(SeasonInfo.Value.Season).AsUniTask().ContinueWith(
                bountyBoard =>
                {
                    BountyBoard.Value = bountyBoard;
                });
        }
    }
}
