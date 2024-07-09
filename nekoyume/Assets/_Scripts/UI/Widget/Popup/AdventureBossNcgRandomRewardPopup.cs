using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;
    public class AdventureBossNcgRandomRewardPopup : PopupWidget
    {
        [SerializeField] private ConditionalButton oKButton;
        [SerializeField] private TextMeshProUGUI exploreWinnerName;
        [SerializeField] private TextMeshProUGUI exploreReward;

        protected override void Awake()
        {
            base.Awake();
            oKButton.OnClickSubject.Subscribe(_ => { Close(); }).AddTo(gameObject);
        }

        public void Show(long seasonId, bool ignoreShowAnimation = false)
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }
            ShowWinner(seasonId);
        }

        public void ShowWinner(long seasonId)
        {
            Game.Game.instance.AdventureBossData.EndedExploreBoards.TryGetValue(seasonId, out var explorerBoard);
            if (explorerBoard == null || string.IsNullOrEmpty(explorerBoard?.RaffleWinnerName))
            {
                //실제로 아무도 참여하지않은 시즌의경우 당첨자가 없을 수 있음.
                NcDebug.LogWarning("RaffleWinnerName is empty");
                return;
            }
            exploreWinnerName.text = explorerBoard.RaffleWinnerName;
            exploreReward.text = explorerBoard.RaffleReward.Value.MajorUnit.ToString("#,0");
            base.Show();
        }
    }
}
