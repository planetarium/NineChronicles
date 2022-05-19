using Nekoyume.L10n;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PaymentPopup : ConfirmPopup
    {
        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        [SerializeField]
        private Image costIcon;

        [SerializeField]
        private TextMeshProUGUI costText;

        public void Show(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract)
        {
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(costType);

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
                        ShowAttract(cost, insufficientMessage, onAttract);
                    }
                }
            };
            Show(popupTitle, enoughMessage, yes, no, false);
        }

        public void ShowAttract(
            BigInteger cost,
            string message,
            System.Action onAttract)
        {
            var title = L10nManager.Localize("UI_TOTAL_COST");
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
