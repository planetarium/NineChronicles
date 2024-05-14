using Nekoyume.Action.AdventureBoss;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
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
        private ConditionalButton receiveAllButton;
        [SerializeField]
        private ConditionalButton[] pageButton;

        private long _targetBlockIndex;
        private List<SeasonInfo> _endedClaimableSeasonInfo = new List<SeasonInfo>();

        public override void Show(bool ignoreShowAnimation = false)
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;
            _endedClaimableSeasonInfo = adventureBossData.EndedSeasonInfos.Values.
                Where(seasonInfo => seasonInfo.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration < Game.Game.instance.Agent.BlockIndex).
                OrderBy(seasonInfo => seasonInfo.EndBlockIndex).
                Take(pageButton.Count()).ToList();

            if (_endedClaimableSeasonInfo.Count == 0)
            {
                Close();
                return;
            }

            if(_endedClaimableSeasonInfo.Count == 1)
            {
                _targetBlockIndex = _endedClaimableSeasonInfo[0].EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration;
                RefreshBlockIndex();
                seasonText.text = $"Season {_endedClaimableSeasonInfo[0].Season}";
                bountyCost.text = $"{_endedClaimableSeasonInfo[0].TotalPoint:#,0}";
            }

            base.Show(ignoreShowAnimation);
        }

        private void RefreshBlockIndex()
        {
            var remainingBlockIndex = _targetBlockIndex - Game.Game.instance.Agent.BlockIndex;
            blockIndex.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
        }
    }
}
