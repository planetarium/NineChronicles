using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena
{
    using UniRx;

    public class ArenaTicketProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private TextMeshProUGUI _sliderText;

        private bool _paused;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Show()
        {
            _slider.normalizedValue = 0;
            _sliderText.text = string.Empty;
            _paused = false;
            _disposables.DisposeAllAndClear();
            UpdateSliderAndText(RxProps.ArenaTicketsProgress.Value);
            RxProps.ArenaTicketsProgress
                .Where(_ => !_paused)
                .SubscribeOnMainThread()
                .Subscribe(UpdateSliderAndText)
                .AddTo(_disposables);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _disposables.DisposeAllAndClear();
            gameObject.SetActive(false);
        }

        public void Pause() => _paused = true;

        public void Resume() => _paused = false;

        public void ResumeOrShow()
        {
            if (_paused)
            {
                Resume();
                return;
            }

            Show();
        }

        private void UpdateSliderAndText(RxProps.TicketProgress ticketProgress)
        {
            _slider.normalizedValue = ticketProgress.NormalizedTicketCount;
            _sliderText.text = L10nManager.Localize(
                "UI_ABOUT",
                ticketProgress.remainTimespanToReset,
                ticketProgress.progressedBlockRange,
                ticketProgress.totalBlockRange);
        }
    }
}
