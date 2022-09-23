using System;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossGradeRewardItem : MonoBehaviour
    {
        public enum Status
        {
            Received,
            Active,
            Normal,
        }

        [SerializeField]
        private TextMeshProUGUI scoreText;

        [SerializeField]
        private TextMeshProUGUI runeCountText;

        [SerializeField]
        private TextMeshProUGUI crystalCountText;

        [SerializeField]
        private GameObject received;

        [SerializeField]
        private GameObject active;

        public void Set(int score, int runeCount, int crystalCount)
        {
            scoreText.text = $"{score:#,0}";
            runeCountText.text = $"{runeCount:#,0}";
            crystalCountText.text = $"{crystalCount:#,0}";
        }

        public void SetStatus(Status status)
        {
            switch (status)
            {
                case Status.Received:
                    received.SetActive(true);
                    active.SetActive(false);
                    break;
                case Status.Active:
                    received.SetActive(false);
                    active.SetActive(true);
                    break;
                case Status.Normal:
                    received.SetActive(false);
                    active.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
