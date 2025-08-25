using System.Globalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Board
{
    public class ArenaBoardBillboard : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _seasonText;

        [SerializeField]
        private TextMeshProUGUI _rankValueText;

        [SerializeField]
        private TextMeshProUGUI _winLoseValueText;

        [SerializeField]
        private TextMeshProUGUI _cpValueText;

        [SerializeField]
        private TextMeshProUGUI _ratingValueText;

        public void SetData(
            string season = "",
            int rank = 0,
            int winCount = 0,
            int loseCount = 0,
            long cp = 0,
            int score = 0)
        {
            _seasonText.text = season;
            _rankValueText.text = rank == 0
                ? "-"
                : rank.ToString("N0", CultureInfo.CurrentCulture);
            _winLoseValueText.text = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0}/{1:N0}",
                winCount,
                loseCount);
            _cpValueText.text = cp.ToString("N0", CultureInfo.CurrentCulture);
            _ratingValueText.text = score.ToString("N0", CultureInfo.CurrentCulture);
        }
    }
}
