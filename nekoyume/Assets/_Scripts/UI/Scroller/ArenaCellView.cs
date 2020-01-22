using System;
using Nekoyume.Helper;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ArenaCellView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rankImageContainer = null;
        [SerializeField]
        private Image rankImage = null;
        [SerializeField]
        private GameObject rankTextContainer = null;
        [SerializeField]
        private TextMeshProUGUI rankText = null;
        [SerializeField]
        private Image portraitImage = null;
        [SerializeField]
        private TextMeshProUGUI levelText = null;
        [SerializeField]
        private TextMeshProUGUI nameText = null;
        [SerializeField]
        private TextMeshProUGUI cpText = null;
        [SerializeField]
        private TextMeshProUGUI scoreText = null;
        [SerializeField]
        private TextMeshProUGUI challengeCountText = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int rank, ArenaInfo arenaInfo)
        {
            if (arenaInfo is null)
                throw new ArgumentNullException(nameof(arenaInfo));
            
            gameObject.SetActive(true);
            
            UpdateRank(rank);
            portraitImage.overrideSprite = SpriteHelper.GetItemIcon(arenaInfo.ArmorId);
            levelText.text = arenaInfo.Level.ToString();
            nameText.text = arenaInfo.AvatarName;
            cpText.text = arenaInfo.CombatPoint.ToString();
            scoreText.text = arenaInfo.Score.ToString();
            challengeCountText.text = $"{arenaInfo.DailyChallengeCount}/{GameConfig.ArenaChallengeCountMax}";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateRank(int rank)
        {
            switch (rank)
            {
                case 1:
                case 2:
                case 3:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(false);
                    rankImage.overrideSprite = SpriteHelper.GetRankIcon(rank); 
                    break;
                default:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }
        }
    }
}
