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
        private Image addCostIcon;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private TextMeshProUGUI addCostText;

        [SerializeField]
        private GameObject addCostContainer;

        public void Show(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract)
        {
            addCostContainer.SetActive(false);
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
                        ShowAttract(costType, cost, insufficientMessage, attractMessage, onAttract);
                    }
                }
            };
            Show(popupTitle, enoughMessage, yes, no, false);
        }

        public void ShowWithAddCost(
            string title,
            string content,
            CostType costType,
            int cost,
            CostType addCostType,
            int addCost,
            System.Action onConfirm)
        {
            addCostContainer.SetActive(true);
            costIcon.overrideSprite = costIconData.GetIcon(costType);
            costText.text = $"{cost:#,0}";

            addCostIcon.overrideSprite = costIconData.GetIcon(addCostType);
            addCostText.text = $"{addCost:#,0}";

            CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    onConfirm?.Invoke();
                }
            };
            Show(title, content);
        }

        public void ShowAttract(
            CostType costType,
            BigInteger cost,
            string content,
            string attractMessage,
            System.Action onAttract) =>
            ShowAttract(costType, cost.ToString(), content, attractMessage, onAttract);

        public void ShowAttract(
            CostType costType,
            string cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            addCostContainer.SetActive(false);
            costIcon.overrideSprite = costIconData.GetIcon(costType);
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
    }
}
