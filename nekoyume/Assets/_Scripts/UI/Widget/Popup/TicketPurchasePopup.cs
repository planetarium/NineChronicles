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
            int purchasedCount,
            int maxPurchaseCount,
            System.Action onConfirm)
        {
            if (purchasedCount < maxPurchaseCount)
            {
                ShowPurchaseTicketPopup(ticketType, costType, balance, cost,
                    purchasedCount, maxPurchaseCount, onConfirm);
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
            System.Action onConfirm)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var enoughBalance = balance >= cost;

            costContainer.SetActive(true);
            costText.text = cost.ToString();
            costText.color = enoughBalance
                ? Color.white
                : Palette.GetColor(EnumType.ColorType.ButtonDisabled);

            maxPurchaseText.text = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);;
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

                Close();
            };
        }

        private void ShowConfirmPopup(CostType ticketType, int purchasedCount, int maxPurchaseCount)
        {
            ticketIcon.overrideSprite = costIconData.GetIcon(ticketType);

            costContainer.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            maxPurchaseText.text = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT",
                purchasedCount, maxPurchaseCount);;
            maxPurchaseText.color = Palette.GetColor(EnumType.ColorType.ButtonDisabled);
            confirmButton.Text = L10nManager.Localize("UI_OK");
            contentText.text = L10nManager.Localize("UI_REACHED_TICKET_BUY_LIMIT", GetTicketName(ticketType));
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
