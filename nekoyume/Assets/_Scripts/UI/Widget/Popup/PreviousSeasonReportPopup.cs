using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        public async UniTaskVoid Show(long seasonIndex, bool ignoreShowAnimation = false)
        {

            base.Show(ignoreShowAnimation);
        }
    }
}
