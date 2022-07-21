using System;
using System.Numerics;
using Nekoyume.Helper;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class WorldBossSeason : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI bossLevelText;

        [SerializeField]
        private TextMeshProUGUI bossNameText;

        [SerializeField]
        private TextMeshProUGUI bossHpText;

        [SerializeField]
        private TextMeshProUGUI bossHpRatioText;

        [SerializeField]
        private Slider bossHpSlider;

        [SerializeField]
        private TextMeshProUGUI raidersText;

        [SerializeField]
        private TextMeshProUGUI myRankText;

        [SerializeField]
        private TextMeshProUGUI myBestRecordText;

        [SerializeField]
        private TextMeshProUGUI myTotalScoreText;

        [SerializeField]
        private Transform gradeContainer;


        private GameObject _gradePrefab;

        public void UpdateUserCount(int count)
        {
            Debug.Log("[WorldBossSeason] UpdateUserCount");
            raidersText.text = $"{count:#,0}";
        }
        public void UpdateBossInformation(string bossName, int level, BigInteger curHp, BigInteger maxHp)
        {
            Debug.Log("[WorldBossSeason] UpdateBossInformation");
            bossNameText.text = bossName;
            bossLevelText.text = $"<size=18>LV.</size>{level}";
            bossHpText.text = $"{curHp:#,0}/{maxHp:#,0}";

            var lCurHp = (long)curHp;
            var lMaxHp = (long)maxHp;

            var ratio = lCurHp / (float)lMaxHp;
            bossHpRatioText.text = $"{(int)(ratio * 100)}%";
            bossHpSlider.normalizedValue = ratio;
        }

        public void UpdateRewards()
        {
            // todo : 미적용
        }

        public void UpdateMyInformation(int highScore, int totalScore)
        {
            Debug.Log("[WorldBossSeason] UpdateMyInformation");
            // todo : 내 랭크 순위 찍어줘야함.
            // todo : 랭크 마크 찍어줘야함.

            if (_gradePrefab != null)
            {
                Destroy(_gradePrefab);
            }

            if (WorldBossFrontHelper.TryGetGrade(WorldBossGrade.S, out var prefab))
            {
                _gradePrefab = Instantiate(prefab, gradeContainer);
            }

            myRankText.text = "loading..";
            myBestRecordText.text = highScore > 0 ? $"{highScore:#,0}" : "-";
            myTotalScoreText.text = totalScore > 0 ? $"{totalScore:#,0}" : "-";
        }
    }
}
