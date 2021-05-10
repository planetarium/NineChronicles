using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BlockTimer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI remainTime = null;

        [SerializeField] private Slider remainTimeSlider = null;

        private readonly List<IDisposable> _disposablesFromOnEnable = new List<IDisposable>();

        private long _expiredTime;

        private void Awake()
        {
            remainTimeSlider.OnValueChangedAsObservable().Subscribe(OnSliderChange)
                .AddTo(gameObject);
            remainTimeSlider.maxValue = Sell.ExpiredBlockIndex;
            remainTimeSlider.value = 0;
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SetBlockIndex)
                .AddTo(_disposablesFromOnEnable);
        }

        private void OnDisable()
        {
            _disposablesFromOnEnable.DisposeAllAndClear();
        }

        private void SetBlockIndex(long blockIndex)
        {
            remainTimeSlider.value = Sell.ExpiredBlockIndex - (_expiredTime - blockIndex);
        }

        private void OnSliderChange(float value)
        {
            var remainSecond = (Sell.ExpiredBlockIndex - value) * 15;
            var timeSpan = TimeSpan.FromSeconds(remainSecond);

            var sb = new StringBuilder();
            if (timeSpan.Hours > 0)
            {
                sb.Append($"{timeSpan.Hours}h");
            }

            if (timeSpan.Minutes > 0)
            {
                if (timeSpan.Hours > 0)
                {
                    sb.Append(" ");
                }

                sb.Append($"{timeSpan.Minutes}m");
            }

            if (sb.Length == 0)
            {
                sb.Append("1m");
            }

            var remainBlock = Sell.ExpiredBlockIndex - value;
            remainTime.text = string.Format(L10nManager.Localize("UI_BLOCK_TIMER"), remainBlock, sb);
        }

        public void UpdateTimer(long expiredTime)
        {
            _expiredTime = expiredTime;
            remainTimeSlider.value = Sell.ExpiredBlockIndex - (_expiredTime - Game.Game.instance.Agent.BlockIndex);
        }
    }
}
