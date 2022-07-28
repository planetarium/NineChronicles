using System;
using System.Collections.Generic;
using Lib9c.Model.Order;
using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class BlockTimer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI remainTime = null;

        [SerializeField] private Slider remainTimeSlider = null;
        [SerializeField] private GameObject expiredText;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private long _expiredTime;

        private void Awake()
        {
            remainTimeSlider.OnValueChangedAsObservable().Subscribe(OnSliderChange)
                .AddTo(gameObject);
            remainTimeSlider.maxValue = Order.ExpirationInterval;
            remainTimeSlider.value = 0;
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SetBlockIndex)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void SetBlockIndex(long blockIndex)
        {
            var value = _expiredTime - blockIndex;
            UpdateUI(value);
        }

        private void OnSliderChange(float value)
        {
            var time = Util.GetBlockToTime((long) value);
            remainTime.text = string.Format(L10nManager.Localize("UI_BLOCK_TIMER"), value, time);
        }

        public void UpdateTimer(long expiredTime)
        {
            _expiredTime = expiredTime;
            var value = _expiredTime - Game.Game.instance.Agent.BlockIndex;
            UpdateUI(value);
        }

        private void UpdateUI(float value)
        {
            if (value > 0)
            {
                expiredText.SetActive(false);
                remainTimeSlider.gameObject.SetActive(true);
                remainTimeSlider.value = value;
            }
            else
            {
                expiredText.SetActive(true);
                remainTimeSlider.gameObject.SetActive(false);
            }
        }
    }
}
