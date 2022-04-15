using Libplanet.Assets;
using Nekoyume.L10n;
using System.Numerics;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class PaymentManager
    {
        public static void ShowPaymentConfirmPopup(
            FungibleAssetValue asset,
            BigInteger cost,
            string usageMessage,
            System.Action onPaymentSucceed)
        {
            var states = Game.Game.instance.States;
            var gold = states.GoldBalanceState.Gold;
            var agentState = states.AgentState;

            // gold
            if (asset.Currency.Equals(gold.Currency))
            {
                if (gold.MajorUnit >= cost)
                {
                    var message = L10nManager.Localize(
                        "UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                        cost,
                        "UI_NCG",
                        usageMessage);

                    var yes = L10nManager.Localize("UI_YES");
                    var no = L10nManager.Localize("UI_NO");
                    Widget.Find<TwoButtonSystem>().Show(message, yes, no, onPaymentSucceed);
                }
                else
                {
                    var message = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                    var ok = L10nManager.Localize("UI_OK");
                    Widget.Find<OneButtonSystem>().Show(message, ok, null);
                }
            }
            else
            {

            }
        }
    }
}
