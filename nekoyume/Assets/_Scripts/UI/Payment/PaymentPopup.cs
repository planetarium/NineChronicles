using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.L10n;
using System.Numerics;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PaymentPopup : ConfirmPopup
    {
        [SerializeField]
        private TextMeshProUGUI costText;

        public const string ConfirmPaymentCurrencyFormat = "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT";

        public void Show(
            FungibleAssetValue asset,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract)
        {
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = asset.MajorUnit >= cost;
            costText.text = cost.ToString();
            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    if (enoughBalance)
                    {
                        onPaymentSucceed.Invoke();
                    }
                    else
                    {
                        ShowAttract(cost, popupTitle, insufficientMessage, onAttract);
                    }
                }
            };
            Show(popupTitle, enoughMessage, yes, no, false);
        }

        public void ShowAttract(
            BigInteger cost,
            string title,
            string message,
            System.Action onAttract)
        {
            costText.text = cost.ToString();
            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    onAttract();
                }
            };
            Show(title, message, yes, no, false);
        }
    }
}
