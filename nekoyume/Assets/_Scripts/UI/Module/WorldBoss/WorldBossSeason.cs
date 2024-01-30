using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;
    public class WorldBossSeason : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI bossLevelText;

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
        private TextMeshProUGUI lastUpdatedText;

        [SerializeField]
        private Transform gradeContainer;

        [SerializeField]
        private GameObject myRankContainer;

        [SerializeField]
        private GameObject apiMissingContainer;

        [SerializeField]
        private GameObject emptyRecordContainer;

        [SerializeField]
        private TouchHandler runeIcon;

        [SerializeField]
        private TouchHandler crystalIcon;

        [SerializeField]
        private GameObject runeInformation;

        [SerializeField]
        private GameObject crystalInformation;

        [SerializeField]
        private List<Image> runeIcons;

        private GameObject _gradeObject;

        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            runeIcon.OnClick
                .Subscribe(_ =>
                {
                    runeInformation.SetActive(!runeInformation.activeSelf);
                    crystalInformation.SetActive(false);
                })
                .AddTo(_disposables);

            crystalIcon.OnClick
                .Subscribe(_ =>
                {
                    crystalInformation.SetActive(!crystalInformation.activeSelf);
                    runeInformation.SetActive(false);
                })
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        public void UpdateUserCount(int count)
        {
            raidersText.text = count > 0 ? $"{count:#,0}" : string.Empty;;
        }
        public void UpdateBossInformation(
            int bossId,
            int level,
            BigInteger curHp,
            BigInteger maxHp)
        {
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
                if (RuneFrontHelper.TryGetRuneStoneIcon(runeRows[i].Ticker, out var sprite))
                {
                    runeIcons[i].sprite = sprite;
                }
            }
        }

        public void PrepareRefresh()
        {
            raidersText.text = string.Empty;
            myTotalScoreText.text = string.Empty;
            myBestRecordText.text = string.Empty;
            myRankText.text = string.Empty;
            lastUpdatedText.text = string.Empty;
        }

        public void UpdateMyInformation(int bossId, WorldBossRankingRecord record, long blockIndex)
        {
            myRankContainer.SetActive(false);
            emptyRecordContainer.SetActive(false);
            apiMissingContainer.SetActive(!Game.Game.instance.ApiClient.IsInitialized);
            if (!Game.Game.instance.ApiClient.IsInitialized)
            {
                return;
            }

            if (record != null)
            {
                myRankContainer.SetActive(true);
                myTotalScoreText.text = $"{record.TotalScore:#,0}";
                myBestRecordText.text = $"{record.HighScore:#,0}";
                myRankText.text = $"{record.Ranking:#,0}";
                lastUpdatedText.text = $"{blockIndex:#,0}";
                UpdateGrade(bossId, record.HighScore);
            }
            else
            {
                emptyRecordContainer.SetActive(true);
            }
        }

        private void UpdateGrade(int bossId, long highScore)
        {
            if (_gradeObject != null)
            {
                Destroy(_gradeObject);
            }

            if (Game.Game.instance.TableSheets.WorldBossCharacterSheet
                .TryGetValue(bossId, out var row))
            {
                var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(row, highScore);
                if (WorldBossFrontHelper.TryGetGrade(grade, false, out var prefab))
                {
                    _gradeObject = Instantiate(prefab, gradeContainer);
                }
            }
        }
    }
}
