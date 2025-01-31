using System;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using GeneratedApiNamespace.ArenaServiceClient;
    using Libplanet.Crypto;
    using Nekoyume.ApiClient;
    using Nekoyume.Blockchain;
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
        private decimal _ticketPrice = 0;

        public ReactiveProperty<bool> IsBuyingTicket = new ReactiveProperty<bool>(false);

        protected override void Awake()
        {
            base.Awake();

            _ticketCountToBuy
                .Subscribe(count =>
                {
                    decimal price = 0;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            price += RxProps.ArenaInfo.Value.BattleTicketStatus.NextNCGCosts[i];
                        }
                    }
                    catch (Exception e)
                    {
                        NcDebug.LogError($"Error calculating ticket price: {e.Message}");
                    }
                    _ticketPrice = price;
                    ticketPriceToBuyText.text = price.ToString();
                    startButton.Interactable = count > 0;
                    willBuyTicketText.text = count.ToString();
                })
                .AddTo(gameObject);

            startButton.OnSubmitSubject.Subscribe(async _ =>
            {
                Close();
                IsBuyingTicket.SetValueAndForceNotify(true);

                var ticketCount = _ticketCountToBuy.Value;
                var goldCurrency = States.Instance.GoldBalanceState.Gold.Currency;
                var cost = Libplanet.Types.Assets.FungibleAssetValue.Parse(goldCurrency, _ticketPrice.ToString());

                var logId = await ActionManager.Instance.TransferAssetsForBattleTicketPurchase(
                    States.Instance.AgentState.address,
                    new Address(RxProps.OperationAccountAddress),
                    ticketCount,
                    cost
                );

                if (logId == -1)
                {
                    NcDebug.LogError("[ArenaTicketPopup] Ticket purchase failed. Please try again later.");
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENATICKET_PURCHASE_FAILED",
                        "UI_OK");
                    IsBuyingTicket.SetValueAndForceNotify(false);
                    return;
                }

                TicketPurchaseLogResponse ticketResponse = null;
                int[] initialPollingIntervals = { 8000, 4000, 2000, 1000 };
                int maxAdditionalAttempts = 30;

                async UniTask<bool> PerformPollingAsync()
                {
                    await ApiClients.Instance.Arenaservicemanager.Client.GetTicketsBattlePurchaselogsAsync(logId, ArenaServiceManager.CreateCurrentJwt(),
                        on200PurchaseLogId: (result) =>
                        {
                            ticketResponse = result;
                        },
                        onError: (error) =>
                        {
                            NcDebug.LogError($"[ArenaTicketPopup] Error while polling for ticket purchase | Error: {error}");
                        }
                    );

                    return ticketResponse != null && ticketResponse.PurchaseStatus == PurchaseStatus.SUCCESS;
                }

                bool isPollingSuccessful = false;
                foreach (var interval in initialPollingIntervals)
                {
                    if (await PerformPollingAsync())
                    {
                        NcDebug.Log("[ArenaTicketPopup] Ticket purchase completed successfully.");
                        isPollingSuccessful = true;
                        break;
                    }
                    await UniTask.Delay(interval);
                }

                for (int i = 0; i < maxAdditionalAttempts && !isPollingSuccessful; i++)
                {
                    if (await PerformPollingAsync())
                    {
                        NcDebug.Log("[ArenaTicketPopup] Ticket purchase completed successfully.");
                        break;
                    }
                    await UniTask.Delay(1000);
                }
                await RxProps.ArenaInfo.UpdateAsync(Game.Game.instance.Agent.BlockTipStateRootHash);
                IsBuyingTicket.SetValueAndForceNotify(false);
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() => Close());
        }

        public void Show()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var ticketCount = RxProps.ArenaInfo.HasValue
                ? RxProps.ArenaInfo.Value.RefreshTicketStatus.RemainingPurchasableTicketsPerRound
                : 0;
            willBuyTicketText.text = "0";

            ticketSlider.Set(0, ticketCount, ticketCount, ticketCount, 1, x => _ticketCountToBuy.Value = x);

            base.Show();
        }
    }
}
