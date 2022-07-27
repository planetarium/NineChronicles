using System;
using System.Collections.Generic;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class EventDungeonTickets : MonoBehaviour
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

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnEnable()
        {
            // TODO: Implement this on the `feature/event-dungeons` branch.
            // RxProps.EventDungeonInfo.Subscribe(info =>
            //     .SubscribeOnMainThread()
            //     .Subscribe(UpdateTimespanText)
            //     .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTimespanText((
            int currentTicketCount,
            int maxTicketCount,
            int progressedBlockRange,
            int totalBlockRange,
            string remainTimespanToReset) tuple)
        {
            var (
                currentTicketCount,
                maxTicketCount,
                _,
                _,
                remainTimespan) = tuple;
            _slider.normalizedValue =
                (float)currentTicketCount / maxTicketCount;
            _fillText.text = $"{currentTicketCount}/{maxTicketCount}";
            _timespanText.text = remainTimespan;
        }
    }
}
