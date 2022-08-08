using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossInformation : WorldBossDetailItem
    {
        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private TextMeshProUGUI bossName;

        [SerializeField]
        private TextMeshProUGUI wave;

        [SerializeField]
        private TextMeshProUGUI content;

        [SerializeField]
        private TextMeshProUGUI hp;

        [SerializeField]
        private TextMeshProUGUI atk;

        [SerializeField]
        private TextMeshProUGUI def;

        [SerializeField]
        private TextMeshProUGUI cri;

        [SerializeField]
        private TextMeshProUGUI hit;

        [SerializeField]
        private Button rightButton;

        [SerializeField]
        private Button leftButton;

        private int _wave;
        private int _bossId;
        private List<WorldBossCharacterSheet.WaveStatData> _cachedData = new();

        private void Awake()
        {
            rightButton.OnClickAsObservable()
                .Subscribe(_ => UpdatePage(1)).AddTo(gameObject);
            leftButton.OnClickAsObservable()
                .Subscribe(_ => UpdatePage(-1)).AddTo(gameObject);
        }

        public void Show()
        {
            var bossId = WorldBossFrontHelper.TryGetCurrentRow(
                Game.Game.instance.Agent.BlockIndex, out var row) ? row.BossId : 0;
            var sheet = Game.Game.instance.TableSheets.WorldBossCharacterSheet;
            if (!sheet.TryGetValue(bossId, out var bossRow))
            {
                return;
            }

            _cachedData = bossRow.WaveStats;
            _wave = 0;
            _bossId = bossId;

            UpdateView();
        }

        private void UpdatePage(int value)
        {
            _wave += value;

            if (_wave >= _cachedData.Count)
            {
                _wave = 0;
            }

            if (_wave < 0)
            {
                _wave = _cachedData.Count - 1;
            }

            UpdateView();
        }

        private void UpdateView()
        {
            if (!WorldBossFrontHelper.TryGetBossData(_bossId, out var bossData))
            {
                return;
            }

            bossImage.sprite = bossData.illustration;
            bossName.text = bossData.name;

            var waveNumber = _wave + 1;
            wave.text = L10nManager.Localize("UI_WAVE_PHASE", waveNumber);
            content.text = L10nManager.Localize($"UI_BOSS_{_bossId}_INFO_WAVE{waveNumber}");

            var data = _cachedData[_wave];
            hp.text = $"{data.HP:#,0}";
            atk.text = $"{data.ATK:#,0}";
            def.text = $"{data.DEF:#,0}";
            cri.text = $"{data.CRI:#,0}";
            hit.text = $"{data.HIT:#,0}";
        }
    }
}
