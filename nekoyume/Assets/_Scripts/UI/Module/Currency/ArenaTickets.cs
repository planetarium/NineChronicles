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

        [SerializeField]
        private Button _chargeButton;

        [SerializeField]
        private GameObject _loadingObj;

        private readonly List<IDisposable> _disposables = new();

        void Awake()
        {
            _chargeButton.onClick.AddListener(OnCharge);
        }

        private void OnCharge()
        {
            Widget.Find<ArenaTicketPopup>().Show();
        }

        private void OnEnable()
        {
            RxProps.ArenaTicketsProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateTimespanText)
                .AddTo(_disposables);
            Widget.Find<ArenaTicketPopup>().IsBuyingTicket
                .SubscribeOnMainThread()
                .Subscribe((isBuying) =>{
                    _fillText.gameObject.SetActive(!isBuying);
                    _loadingObj.SetActive(isBuying);
                    _chargeButton.interactable = !isBuying;
                })
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
            _timespanText.text = ticketProgress.remainTimespanToReset;
        }
    }
}
