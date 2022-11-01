using Nekoyume.L10n;
using Libplanet.Assets;
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
            System.Action goToMarget,
            int seasonPurchased,
            int maxSeasonPurchased,
            int intervalPurchased,
            int maxIntervalPurchased)
        {
            var remainSeasonPurchased = maxSeasonPurchased - seasonPurchased;
            var remainIntervalPurchased = maxIntervalPurchased - intervalPurchased;

            if (remainSeasonPurchased <= 0)
            {
                ShowConfirmPopup(ticketType, seasonPurchased, maxSeasonPurchased, false);
            }
            else if (remainIntervalPurchased <= 0)
            {
                ShowConfirmPopup(ticketType, intervalPurchased, maxIntervalPurchased, true, true);
            }
            else if (remainSeasonPurchased == remainIntervalPurchased)
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    seasonPurchased, maxSeasonPurchased, onConfirm, goToMarget);
            }
            else
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    intervalPurchased, maxIntervalPurchased, onConfirm, goToMarget, true);
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
            maxPurchaseText.color = Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            maxPurchaseText.text = L10nManager.Localize(
                "UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);

            contentText.text = L10nManager.Localize(
                isInternalLimit ? "UI_REACHED_TICKET_BUY_INTERVAL_LIMIT" : "UI_REACHED_TICKET_BUY_LIMIT",
                GetTicketName(ticketType));

            remainingContainer.SetActive(showRemaining);
            if (showRemaining)
            {
                var ticketProgress = RxProps.ArenaTicketsProgress.Value; // Todo : remaining 표시
                var progressed = ticketProgress.progressedBlockRange;
                var total = ticketProgress.totalBlockRange;

                remainingContainer.SetActive(true);
                remainingSlider.thresholdRatio = (float)progressed / total;
                remainingText.text =
                    $"{total - progressed}({Util.GetBlockToTime(total - progressed)})`";
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
            System.Action goToMarget,
            bool showRemaining = false)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var enoughBalance = balance >= cost;

            remainingContainer.SetActive(false);
            costContainer.SetActive(true);
            costText.text = cost.MinorUnit > 0
                ? $"{cost.MajorUnit:#,0}.{cost.MinorUnit}"
                : $"{cost.MajorUnit:#,0}";
            costText.color = enoughBalance
                ? Color.white
                : Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            var purchaseMessage = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);
            if (showRemaining)
            {
                var remaining = RxProps.ArenaTicketsProgress.Value.remainTimespanToReset;
                purchaseMessage = $"{purchaseMessage}\n({remaining})";
            }

            maxPurchaseText.text = purchaseMessage;
            maxPurchaseText.color = Palette.GetColor(EnumType.ColorType.TextElement06);

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
                    Find<PaymentPopup>().ShowAttract(
                        CostType.NCG,
                        cost.GetQuantityString(),
                        L10nManager.Localize("UI_NOT_ENOUGH_NCG_WITH_SUPPLIER_INFO"),
                        L10nManager.Localize("UI_GO_TO_MARKET"),
                        goToMarget);
                }

                Close(!enoughBalance);
            };
        }
    }
}
