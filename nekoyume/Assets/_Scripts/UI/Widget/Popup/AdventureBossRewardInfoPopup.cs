using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.Model.AdventureBoss;
    using TMPro;
    using UniRx;
    public class AdventureBossRewardInfoPopup : PopupWidget
    {
        [SerializeField] private UnityEngine.UI.ToggleGroup toggleGroup;
        [SerializeField] private Toggle toggleScore;
        [SerializeField] private Toggle toggleFloor;
        [SerializeField] private Toggle toggleOperational;
        [SerializeField] private GameObject contentsScore;
        [SerializeField] private GameObject contentsFloor;
        [SerializeField] private GameObject contentsOperational;
        [SerializeField] private TextMeshProUGUI remainingBlockTime;

        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private long _seasonEndBlock;

        override protected void Awake()
        {
            toggleScore.onValueChanged.AddListener((isOn) =>
            {
                contentsScore.SetActive(isOn);
            });
            toggleFloor.onValueChanged.AddListener((isOn) =>
            {
                contentsFloor.SetActive(isOn);
            });
            toggleOperational.onValueChanged.AddListener((isOn) =>
            {
                contentsOperational.SetActive(isOn);
            }); 
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Game.Game.instance.AdventureBossData.SeasonInfo.
                Subscribe(RefreshSeasonInfo).
                AddTo(_disposablesByEnable);
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);
            base.Show(ignoreShowAnimation);
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            if (seasonInfo == null)
            {
                return;
            }
            _seasonEndBlock = seasonInfo.EndBlockIndex;
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var remainingBlockIndex = _seasonEndBlock - blockIndex;
            if (remainingBlockIndex < 0)
            {
                Close();
                return;
            }
            remainingBlockTime.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
        }
    }
}
