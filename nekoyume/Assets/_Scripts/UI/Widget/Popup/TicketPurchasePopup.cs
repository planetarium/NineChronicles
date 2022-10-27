using Nekoyume.L10n;
using Libplanet.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class TicketPurchasePopup : PopupWidget
    {
        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        [SerializeField]
        private Image ticketIcon;

        [SerializeField]
        private Image costIcon;

        [SerializeField]
        private GameObject costContainer;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI maxPurchaseText;

        [SerializeField]
        private TextButton confirmButton;

        [SerializeField]
        private TextButton cancelButton;

        protected override void Awake()
        {
            base.Awake();
            cancelButton.Text = L10nManager.Localize("UI_NO");
            cancelButton.OnClick = () => Close();
        }

        public void Show(
            CostType ticketType,
            CostType costType,
            FungibleAssetValue balance,
            FungibleAssetValue cost,
            System.Action onConfirm,
            System.Action goToMarget,
            (int current, int max) seasonPurchased,
            (int current, int max)? intervalPurchased = null)
        {
            if (seasonPurchased.current >= seasonPurchased.max)
            {
                ShowConfirmPopup(ticketType, (seasonPurchased.current, seasonPurchased.max));

            }
            else if (intervalPurchased != null &&
                     intervalPurchased.Value.current >= intervalPurchased.Value.max)
            {
                ShowConfirmPopup(ticketType, (seasonPurchased.current, seasonPurchased.max),
                    (intervalPurchased.Value.current, intervalPurchased.Value.max));
            }
            else
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    onConfirm, goToMarget, seasonPurchased, intervalPurchased);
            }

            Show();
        }

        private void ShowPurchaseTicketPopup(
            CostType ticketType,
            CostType costType,
            FungibleAssetValue balance,
            FungibleAssetValue cost,
            System.Action onConfirm,
            System.Action goToMarget,
            (int current, int max) seasonPurchased,
            (int current, int max)? intervalPurchased = null)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var enoughBalance = balance >= cost;

            costContainer.SetActive(true);
            costText.text = cost.MinorUnit > 0
                ? $"{cost.MajorUnit:#,0}.{cost.MinorUnit}"
                : $"{cost.MajorUnit:#,0}";
            costText.color = enoughBalance
                ? Color.white
                : Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            var purchaseMessage = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                seasonPurchased.current, seasonPurchased.max);
            if (intervalPurchased != null)
            {
                var interval = L10nManager.Localize("UI_TICKET_INTERVAL_PURCHASE_LIMIT",
                    intervalPurchased.Value.current, intervalPurchased.Value.max);
                purchaseMessage = $"{purchaseMessage}\n{interval}";
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

        private void ShowConfirmPopup(
            CostType ticketType,
            (int current, int max) seasonPurchased,
            (int current, int max)? intervalPurchased = null)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);

            costContainer.SetActive(false);
            cancelButton.gameObject.SetActive(false);

            var purchaseMessage = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                seasonPurchased.current, seasonPurchased.max);
            if (intervalPurchased != null)
            {
                var interval = L10nManager.Localize("UI_TICKET_INTERVAL_PURCHASE_LIMIT",
                    intervalPurchased.Value.current, intervalPurchased.Value.max);
                purchaseMessage = $"{purchaseMessage}\n{interval}";
            }

            maxPurchaseText.text = purchaseMessage;
            maxPurchaseText.color = Palette.GetColor(EnumType.ColorType.ButtonDisabled);
            confirmButton.Text = L10nManager.Localize("UI_OK");
            contentText.text = intervalPurchased != null
                ? L10nManager.Localize("UI_REACHED_TICKET_BUY_INTERVAL_LIMIT", GetTicketName(ticketType))
                : L10nManager.Localize("UI_REACHED_TICKET_BUY_LIMIT", GetTicketName(ticketType));
            confirmButton.OnClick = () => Close();
        }

        private string GetTicketName(CostType ticketType)
        {
            return ticketType switch
            {
                CostType.ArenaTicket => L10nManager.Localize("UI_ARENA_JOIN_BACK_BUTTON"),
                CostType.WorldBossTicket => L10nManager.Localize("UI_WORLD_BOSS"),
                _ => string.Empty
            };
        }
    }
}
