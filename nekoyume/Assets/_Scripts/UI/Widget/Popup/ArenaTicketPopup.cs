using System;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class ArenaTicketPopup : PopupWidget
    {
        [SerializeField]
        private SweepSlider ticketSlider = null;

        [SerializeField]
        private TextMeshProUGUI willBuyTicketText = null;

        [SerializeField]
        private TextMeshProUGUI ticketPriceToBuyText = null;

        [SerializeField]
        private ConditionalButton startButton = null;

        [SerializeField]
        private Button closeButton = null;

        private readonly ReactiveProperty<int> _ticketCountToBuy = new();

        protected override void Awake()
        {
            base.Awake();

            _ticketCountToBuy
                .Subscribe(count =>
                {
                    var price = 0;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            price += (int)RxProps.ArenaInfo.Value.BattleTicketStatus.NextNCGCosts[i];
                        }
                    }
                    catch (Exception e)
                    {
                        NcDebug.LogError($"Error calculating ticket price: {e.Message}");
                    }
                    ticketPriceToBuyText.text = price.ToString();
                    startButton.Interactable = count > 0;
                })
                .AddTo(gameObject);

            startButton.OnSubmitSubject.Subscribe(_ =>
            {

                Close();
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() => Close());
        }

        public void Show()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var ticketCount = RxProps.ArenaInfo.HasValue
                ? RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingPurchasableTicketsPerRound
                : 0;
            willBuyTicketText.text = ticketCount.ToString();

            ticketSlider.Set(0, ticketCount, ticketCount, ticketCount, 1, x => _ticketCountToBuy.Value = x);

            base.Show();
        }
    }
}
