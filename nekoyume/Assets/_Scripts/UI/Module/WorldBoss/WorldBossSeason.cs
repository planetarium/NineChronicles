using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.ApiClient;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.State;
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
            myTotalScoreText.text = string.Empty;
            myBestRecordText.text = string.Empty;
            lastUpdatedText.text = string.Empty;
        }

        public void UpdateMyInformation(int bossId, RaiderState raider)
        {
            myRankContainer.SetActive(false);
            emptyRecordContainer.SetActive(false);

            if (raider != null)
            {
                myRankContainer.SetActive(true);
                myTotalScoreText.text = $"{raider.TotalScore:#,0}";
                myBestRecordText.text = $"{raider.HighScore:#,0}";
                lastUpdatedText.text = $"{raider.UpdatedBlockIndex:#,0}";
                UpdateGrade(bossId, raider.HighScore);
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

            if (!Game.Game.instance.TableSheets.WorldBossCharacterSheet.TryGetValue(bossId, out var row))
            {
                return;
            }

            var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(row, highScore);
            if (WorldBossFrontHelper.TryGetGrade(grade, false, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeContainer);
            }
        }
    }
}
