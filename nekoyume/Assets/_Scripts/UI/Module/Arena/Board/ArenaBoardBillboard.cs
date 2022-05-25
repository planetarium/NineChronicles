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
            string season,
            string rank,
            string winLose,
            string cp,
            string rating)
        {
            _seasonText.text = season;
            _rankValueText.text = rank;
            _winLoseValueText.text = winLose;
            _cpValueText.text = cp;
            _ratingValueText.text = rating;
        }
    }
}
