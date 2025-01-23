using System;
using System.Collections.Generic;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ArenaTickets : MonoBehaviour
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
            Game.Game.instance.Agent.BlockIndexSubject
                .StartWith(Game.Game.instance.Agent.BlockIndex)
                .Subscribe(RxProps.UpdateArenaTicketProgress)
                .AddTo(_disposables);

            RxProps.ArenaTicketsProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateTimespanText)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTimespanText(RxProps.TicketProgress ticketProgress)
        {
            _slider.normalizedValue = ticketProgress.NormalizedTicketCount;
            _fillText.text = ticketProgress.CurrentAndMaxTicketCountText;
            _timespanText.text = ticketProgress.remainTimespanToReset;
        }
    }
}
