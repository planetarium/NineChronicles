using Nekoyume.L10n;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PaymentPopup : ConfirmPopup
    {
        [SerializeField] private CostIconDataScriptableObject costIconData;

        [SerializeField] private Image costIcon;

        [SerializeField] private TextMeshProUGUI costText;

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
                        var attractMessage = costType == CostType.Crystal
                            ? L10nManager.Localize("UI_GO_GRINDING")
                            : L10nManager.Localize("UI_YES");
                        ShowAttract(cost, insufficientMessage, attractMessage, onAttract);
                    }
                }
            };
            Show(popupTitle, enoughMessage, yes, no, false);
        }

        public void ShowAttract(
            BigInteger cost,
            string content,
            string attractMessage,
            System.Action onAttract) =>
            ShowAttract(cost.ToString(), content, attractMessage, onAttract);

        public void ShowAttract(
            string cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            var title = L10nManager.Localize("UI_TOTAL_COST");
            costText.text = cost;
            var no = L10nManager.Localize("UI_NO");
            CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    onAttract();
                }
            };
            Show(title, content, attractMessage, no, false);
        }

        public void ShowAttract(
            CostType costType,
            string cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            costIcon.overrideSprite = costIconData.GetIcon(costType);
            ShowAttract(cost, content, attractMessage, onAttract);
        }
    }
}