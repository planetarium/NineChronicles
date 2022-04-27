using Libplanet.Assets;
using Nekoyume.L10n;
using System.Numerics;
using TMPro;

namespace Nekoyume.UI
{
    public class PaymentPopup : ConfirmPopup
    {
        public TextMeshProUGUI costText;

        public void Show(
            FungibleAssetValue balance,
            BigInteger cost,
            string usageMessage,
            System.Action onPaymentSucceed)
        {
            // gold
            if (balance.Currency.Equals(
                Game.Game.instance.States.GoldBalanceState.Gold.Currency))
            {
                var ncgText = L10nManager.Localize("UI_NCG");
                var enoughMessage = L10nManager.Localize(
                    "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                    cost,
                    ncgText,
                    usageMessage);
                var insufficientMessage = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                ShowInternal(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed);
            }
            // crystal
            else if (balance.Currency.Equals(new Currency("CRYSTAL", 18, minters: null)))
            {
                var crystalText = L10nManager.Localize("UI_CRYSTAL");
                var enoughMessage = L10nManager.Localize(
                    "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                    cost,
                    crystalText,
                    usageMessage);
                var insufficientMessage = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                ShowInternal(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed);
            }
        }

        private void ShowInternal(
            FungibleAssetValue asset,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed)
        {
            var title = L10nManager.Localize("UI_TOTAL_COST");
            costText.text = cost.ToString();
            if (asset.MajorUnit >= cost)
            {
                var yes = L10nManager.Localize("UI_YES");
                var no = L10nManager.Localize("UI_NO");
                CloseCallback = result =>
                {
                    if (result == ConfirmResult.Yes)
                    {
                        onPaymentSucceed();
                    }
                };
                Show(title, enoughMessage, yes, no, false);
            }
            else
            {
                var yes = L10nManager.Localize("UI_YES");
                var no = L10nManager.Localize("UI_NO");
                CloseCallback = null;
                Show(title, insufficientMessage, yes, no, false);
            }
        }
    }
}
