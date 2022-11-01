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
        protected CostIconDataScriptableObject costIconData;

        [SerializeField]
        protected Image ticketIcon;

        [SerializeField]
        protected Image costIcon;

        [SerializeField]
        protected GameObject costContainer;

        [SerializeField]
        protected TextMeshProUGUI costText;

        [SerializeField]
        protected TextMeshProUGUI contentText;

        [SerializeField]
        protected TextMeshProUGUI maxPurchaseText;

        [SerializeField]
        protected TextButton confirmButton;

        [SerializeField]
        protected TextButton cancelButton;

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
            int purchasedCount,
            int maxPurchaseCount,
            System.Action onConfirm,
            System.Action goToMarget)
        {
            if (purchasedCount < maxPurchaseCount)
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    purchasedCount, maxPurchaseCount, onConfirm, goToMarget);
            }
            else
            {
                ShowConfirmPopup(ticketType, purchasedCount, maxPurchaseCount);
            }

            Show();
        }

        private void ShowPurchaseTicketPopup(
            CostType ticketType,
            CostType costType,
            FungibleAssetValue balance,
            FungibleAssetValue cost,
            int purchasedCount,
            int maxPurchaseCount,
            System.Action onConfirm,
            System.Action goToMarget)
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

            maxPurchaseText.text = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);
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

        private void ShowConfirmPopup(CostType ticketType, int purchasedCount, int maxPurchaseCount)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);

            costContainer.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            maxPurchaseText.text = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);
            maxPurchaseText.color = Palette.GetColor(EnumType.ColorType.ButtonDisabled);
            confirmButton.Text = L10nManager.Localize("UI_OK");
            contentText.text = L10nManager.Localize("UI_REACHED_TICKET_BUY_LIMIT", GetTicketName(ticketType));
            confirmButton.OnClick = () => Close();
        }

        protected static string GetTicketName(CostType ticketType)
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
