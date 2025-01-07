using Nekoyume.L10n;
using Libplanet.Types.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaTicketPurchasePopup : TicketPurchasePopup
    {
        [SerializeField] private GameObject remainingContainer;

        [SerializeField] private ClampableSlider remainingSlider;

        [SerializeField] private TextMeshProUGUI remainingText;

        public void Show(
            CostType ticketType,
            CostType costType,
            FungibleAssetValue balance,
            FungibleAssetValue cost,
            System.Action onConfirm,
            int seasonPurchasedCount,
            int maxSeasonPurchaseCount,
            int intervalPurchasedCount,
            int maxIntervalPurchaseCount)
        {
            var remainSeasonPurchaseCount = maxSeasonPurchaseCount - seasonPurchasedCount;
            var remainIntervalPurchaseCount = maxIntervalPurchaseCount - intervalPurchasedCount;

            if (remainSeasonPurchaseCount <= 0)
            {
                ShowConfirmPopup(ticketType, seasonPurchasedCount, maxSeasonPurchaseCount, false);
            }
            else if (remainIntervalPurchaseCount <= 0)
            {
                ShowConfirmPopup(ticketType, intervalPurchasedCount, maxIntervalPurchaseCount, true, true);
            }
            else if (remainIntervalPurchaseCount > remainSeasonPurchaseCount)
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    seasonPurchasedCount, maxSeasonPurchaseCount, onConfirm, false);
            }
            else
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    intervalPurchasedCount, maxIntervalPurchaseCount, onConfirm, true, true);
            }

            Show();
        }

        private void ShowConfirmPopup(
            CostType ticketType,
            int purchasedCount,
            int maxPurchaseCount,
            bool isInternalLimit,
            bool showRemaining = false)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);
            var purchaseMessage = L10nManager.Localize(
                isInternalLimit ? "UI_TICKET_INTERVAL_PURCHASE_LIMIT" : "UI_TICKET_PURCHASE_LIMIT");
            purchaseMessage = $"{purchaseMessage} {purchasedCount}/{maxPurchaseCount}";
            purchaseText.text = purchaseMessage;
            purchaseText.color = Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            contentText.text = L10nManager.Localize(
                isInternalLimit ? "UI_REACHED_TICKET_BUY_INTERVAL_LIMIT" : "UI_REACHED_TICKET_BUY_LIMIT",
                GetTicketName(ticketType));

            remainingContainer.SetActive(showRemaining);
            if (showRemaining)
            {
                // TODO: 아레나 서비스 완성 후 구현
                // var ticketProgress = RxProps.ArenaTicketsProgress.Value; // Todo : remaining 표시
                var ticketProgress = new RxProps.TicketProgress();
                var progressed = ticketProgress.progressedBlockRange;
                var total = ticketProgress.totalBlockRange;
                long remaining = total - progressed;

                remainingContainer.SetActive(true);
                remainingSlider.thresholdRatio = (float)progressed / total;
                remainingText.text = $"{remaining}({remaining.BlockRangeToTimeSpanString()})";
            }

            costContainer.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            confirmButton.Text = L10nManager.Localize("UI_OK");
            confirmButton.OnClick = () => Close();
        }

        private void ShowPurchaseTicketPopup(
            CostType ticketType,
            CostType costType,
            FungibleAssetValue balance,
            FungibleAssetValue cost,
            int purchasedCount,
            int maxPurchaseCount,
            System.Action onConfirm,
            bool isInternalLimit,
            bool showRemaining = false)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var enoughBalance = balance >= cost;

            remainingContainer.SetActive(false);
            costContainer.SetActive(true);
            costText.text = cost.GetQuantityString();
            costText.color = enoughBalance
                ? Color.white
                : Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            var purchaseMessage = L10nManager.Localize(
                isInternalLimit ? "UI_TICKET_INTERVAL_PURCHASE_LIMIT" : "UI_TICKET_PURCHASE_LIMIT");
            purchaseMessage = $"{purchaseMessage} {purchasedCount}/{maxPurchaseCount}";
            if (showRemaining)
            {
                // TODO: 아레나 서비스 완성 후 구현
                // var ticketProgress = RxProps.ArenaTicketsProgress.Value;
                var ticketProgress = new RxProps.TicketProgress();
                long remaining = ticketProgress.totalBlockRange - ticketProgress.progressedBlockRange;
                purchaseMessage = $"{purchaseMessage}\n({remaining.BlockRangeToTimeSpanString()})";
            }

            purchaseText.text = purchaseMessage;
            purchaseText.color = Palette.GetColor(EnumType.ColorType.TextElement06);

            cancelButton.gameObject.SetActive(true);
            contentText.text = L10nManager.Localize("UI_BUY_TICKET_AND_START", GetTicketName(ticketType));
            confirmButton.Text = L10nManager.Localize("UI_YES");
            confirmButton.OnClick = () =>
            {
                if (enoughBalance)
                {
                    onConfirm?.Invoke();
                }
                else
                {
                    Find<PaymentPopup>().ShowLackPaymentNCG(cost.GetQuantityString());
                }

                Close(!enoughBalance);
            };
        }
    }
}
