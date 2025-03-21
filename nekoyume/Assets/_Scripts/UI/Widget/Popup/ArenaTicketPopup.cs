﻿using System;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using System.Globalization;
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

        [SerializeField]
        private TextMeshProUGUI ticketTotalCountText = null;

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
                        var currentCount = RxProps.ArenaInfo.Value.BattleTicketStatus.TicketsPurchasedPerSeason;
                        var totalCount = RxProps.ArenaInfo.Value.BattleTicketStatus.RemainingPurchasableTicketsPerSeason + currentCount;
                        ticketTotalCountText.text = $"<color=#FFD700>{currentCount}</color> <color=#32CD32>+ {count}</color> <color=#CCCCCC>/</color> <color=#FFD700>{totalCount}</color>";
                    }
                    catch (Exception e)
                    {
                        NcDebug.LogError($"Error calculating ticket price: {e.Message}");
                        ticketTotalCountText.text = string.Empty;
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
                var cost = Libplanet.Types.Assets.FungibleAssetValue.Parse(goldCurrency, _ticketPrice.ToString(CultureInfo.InvariantCulture));

                if (States.Instance.GoldBalanceState.Gold < cost)
                {
                    NcDebug.LogError("[ArenaTicketPopup] Ticket purchase failed. Not Enough Cost");
                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        "UI_ARENATICKET_NOT_ENOUGH_GOLD",
                        "UI_OK");
                    IsBuyingTicket.SetValueAndForceNotify(false);
                    return;
                }
                int logId = -1;
                try
                {
                    logId = await ActionManager.Instance.TransferAssetsForBattleTicketPurchase(
                        States.Instance.AgentState.address,
                        new Address(RxProps.OperationAccountAddress),
                        ticketCount,
                        cost
                    );
                }
                catch (Exception e)
                {
                    NcDebug.LogError($"[ArenaTicketPopup] 티켓 구매 중 예외 발생: {e.Message}");

                    Find<IconAndButtonSystem>().Show(
                        "UI_ERROR",
                        e.InnerException != null ? e.InnerException.Message : e.Message,
                        "UI_OK");
                    IsBuyingTicket.SetValueAndForceNotify(false);
                    return;
                }

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
                        on200: (result) =>
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
            var ticketMaxCount = RxProps.ArenaInfo != null && RxProps.ArenaInfo.HasValue
                ? RxProps.ArenaInfo.Value.BattleTicketStatus.RemainingPurchasableTicketsPerRound
                : 0;
            var ticketCount = ticketMaxCount > 0 ? 1 : 0;
            willBuyTicketText.text = ticketCount.ToString();
            _ticketCountToBuy.SetValueAndForceNotify(ticketCount);
            ticketSlider.Set(0, ticketMaxCount, ticketCount, ticketMaxCount, 1, x => _ticketCountToBuy.Value = x);
            base.Show();
        }
    }
}
