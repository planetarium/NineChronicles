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

        public void Show(
            FungibleAssetValue balance,
            BigInteger cost,
            string usageMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract,
            bool passToAttraction = true,
            bool useDefaultPaymentFormat = true)
        {
            // gold
            if (balance.Currency.Equals(
                Game.Game.instance.States.GoldBalanceState.Gold.Currency))
            {
                var ncgText = L10nManager.Localize("UI_NCG");
                var enoughMessage = useDefaultPaymentFormat
                    ? L10nManager.Localize(
                        "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                        cost,
                        ncgText,
                        usageMessage)
                    : usageMessage;
                var insufficientMessage = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                ShowInternal(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed, onAttract, passToAttraction);
            }
            // crystal
            else if (balance.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                var crystalText = L10nManager.Localize("UI_CRYSTAL");
                var enoughMessage = useDefaultPaymentFormat
                    ? L10nManager.Localize(
                        "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                        cost,
                        crystalText,
                        usageMessage)
                    : usageMessage;
                var insufficientMessage = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                ShowInternal(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed, onAttract, passToAttraction);
            }
        }

        private void ShowInternal(
            FungibleAssetValue asset,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract,
            bool passToAttraction)
        {
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = asset.MajorUnit >= cost;
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
            costText.text = cost.ToString();
            if (passToAttraction && !enoughBalance)
            {
                ShowAttract(cost, popupTitle, insufficientMessage, onAttract);
            }
            else
            {
                Show(popupTitle, enoughMessage, yes, no, false);
            }
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
