using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
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

        [SerializeField]
        private List<Image> runeIcons;

        private GameObject _gradeObject;

        public void UpdateUserCount(int count)
        {
            Debug.Log("[WorldBossSeason] UpdateUserCount");
            raidersText.text = count > 0 ? $"{count:#,0}" : "-";
        }
        public void UpdateBossInformation(
            int bossId,
            string bossName,
            int level,
            BigInteger curHp,
            BigInteger maxHp)
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

            UpdateRewards(bossId);
        }

        private void UpdateRewards(int bossId)
        {
            if (!WorldBossFrontHelper.TryGetRunes(bossId, out var runeRows))
            {
                return;
            }

            for (var i = 0; i < runeRows.Count; i++)
            {
                if (WorldBossFrontHelper.TryGetRuneIcon(runeRows[i].Ticker, out var sprite))
                {
                    runeIcons[i].sprite = sprite;
                }
            }
        }

        public void UpdateMyInformation(int totalScore, int highScore, int rank)
        {
            Debug.Log("[WorldBossSeason] UpdateMyInformation");

            myTotalScoreText.text = totalScore > 0 ? $"{totalScore:#,0}" : "-";
            myBestRecordText.text = highScore > 0 ? $"{highScore:#,0}" : "-";
            myRankText.text = rank > 0 ? $"{rank:#,0}" : "-";

            UpdateGrade(highScore);
        }

        private void UpdateGrade(int highScore)
        {
            if (_gradeObject != null)
            {
                Destroy(_gradeObject);
            }

            var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(highScore);
            if (WorldBossFrontHelper.TryGetGrade(grade, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeContainer);
            }
        }
    }
}
