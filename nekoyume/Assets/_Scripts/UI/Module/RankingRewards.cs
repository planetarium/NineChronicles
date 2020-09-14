using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RankingRewards : MonoBehaviour
    {
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
            firstRewardText.text = GameConfig.RankingRewardFirst.ToString();
            secondRewardText.text = GameConfig.RankingRewardSecond.ToString();
            thirdRewardText.text = GameConfig.RankingRewardThird.ToString();
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
