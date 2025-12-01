using System;
using System.Collections.Generic;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class InfiniteTowerTickets : MonoBehaviour
    {
        [SerializeField]
        private Image _iconImage;

        public Image IconImage => _iconImage;

        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private TextMeshProUGUI _fillText;

        [SerializeField]
        private TextMeshProUGUI _timespanText;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            RxProps.InfiniteTowerTicketProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateTimespanText)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTimespanText(RxProps.TicketProgress ticketProgress)
        {
            _slider.normalizedValue = ticketProgress.NormalizedTicketCount;
            _fillText.text = ticketProgress.CurrentAndMaxTicketCountText;

            // 빈 문자열인 경우 timespan 텍스트 비활성화
            // (아직 리필이 한 번도 발생하지 않은 경우)
            if (string.IsNullOrEmpty(ticketProgress.remainTimespanToReset))
            {
                _timespanText.text = string.Empty;
                _timespanText.gameObject.SetActive(false);
            }
            else
            {
                _timespanText.text = ticketProgress.remainTimespanToReset;
                _timespanText.gameObject.SetActive(true);
            }
        }
    }
}
