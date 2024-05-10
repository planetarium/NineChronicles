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
        public ReactiveProperty<SeasonInfo> CurrentSeasonInfo = new ReactiveProperty<SeasonInfo>();
        public ReactiveProperty<BountyBoard> CurrentBountyBoard = new ReactiveProperty<BountyBoard>();

        public void Initialize()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(_ =>
            {
                if (CurrentSeasonInfo.Value == null || CurrentSeasonInfo.Value.EndBlockIndex < Game.Game.instance.Agent.BlockIndex)
                {
                    RefreshSeasonInfo();
                }
            });
        }

        public void RefreshSeasonInfo()
        {
            Game.Game.instance.Agent.GetAdventureBossLatestSeasonInfoAsync().AsUniTask().ContinueWith(
                latestSeason =>
                {
                    CurrentSeasonInfo.Value = latestSeason;
                    RefreshBountyBoard();
                });
        }

        public void RefreshBountyBoard()
        {
            if(CurrentSeasonInfo.Value == null)
            {
                return;
            }
            Game.Game.instance.Agent.GetBountyBoardAsync(CurrentSeasonInfo.Value.Season).AsUniTask().ContinueWith(
                bountyBoard =>
                {
                    CurrentBountyBoard.Value = bountyBoard;
                });
        }
    }
}
