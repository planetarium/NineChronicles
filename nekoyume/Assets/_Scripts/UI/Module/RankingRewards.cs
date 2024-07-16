using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RankingRewards : MonoBehaviour
    {
        public const int RankingRewardFirst = 50;
        public const int RankingRewardSecond = 30;
        public const int RankingRewardThird = 10;

        [SerializeField]
        private TextMeshProUGUI descriptionText = null;

        [SerializeField]
        private TextMeshProUGUI firstRewardText = null;

        [SerializeField]
        private TextMeshProUGUI secondRewardText = null;

        [SerializeField]
        private TextMeshProUGUI thirdRewardText = null;

        private void Awake()
        {
            descriptionText.text = L10nManager.Localize("UI_RANKING_REWARDS_DESCRIPTION");
            firstRewardText.text = RankingRewardFirst.ToString();
            secondRewardText.text = RankingRewardSecond.ToString();
            thirdRewardText.text = RankingRewardThird.ToString();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
