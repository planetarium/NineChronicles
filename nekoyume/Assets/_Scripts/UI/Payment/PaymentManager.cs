using Libplanet.Assets;
using Nekoyume.L10n;
using System.Numerics;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class PaymentManager
    {
        public static void ConfirmPayment(
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
                ShowPaymentPopup(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed);
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
                ShowPaymentPopup(balance, cost, enoughMessage, insufficientMessage, onPaymentSucceed);
            }
        }

        private static void ShowPaymentPopup(
            FungibleAssetValue asset,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed)
        {
            if (asset.MajorUnit >= cost)
            {
                var yes = L10nManager.Localize("UI_YES");
                var no = L10nManager.Localize("UI_NO");
                Widget.Find<TwoButtonSystem>().Show(enoughMessage, yes, no, onPaymentSucceed);
            }
            else
            {
                var ok = L10nManager.Localize("UI_OK");
                Widget.Find<OneButtonSystem>().Show(insufficientMessage, ok, null);
            }
        }
    }
}
