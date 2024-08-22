using Nekoyume.L10n;
using System.Numerics;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PaymentPopup : PopupWidget
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
        
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        public TextButton buttonYes;
        public TextButton buttonNo;
        public GameObject titleBorder;
        public ConfirmDelegate CloseCallback { get; set; }
        
        protected override void Awake()
        {
            base.Awake();

            buttonNo.OnClick = No;
            buttonYes.OnClick = Yes;
            CloseWidget = NoWithoutCallback;
            SubmitWidget = Yes;
        }

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
                        Close(true);
                        var attractMessage = costType == CostType.Crystal
                            ? L10nManager.Localize("UI_GO_GRINDING")
                            : L10nManager.Localize("UI_YES");
                        ShowAttract(costType, cost, insufficientMessage, attractMessage, onAttract);
                    }
                }
            };
            Show(popupTitle, enoughMessage, yes, no, false);
        }
        

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            Set(title, content, labelYes, labelNo, localize);

            if (!gameObject.activeSelf)
            {
                Show();
            }
        }

        public void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            var titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                {
                    this.title.text = L10nManager.Localize(title);
                }

                this.content.text = L10nManager.Localize(content);
                buttonYes.Text = L10nManager.Localize(labelYes);
                buttonNo.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                buttonYes.Text = labelYes;
                buttonNo.Text = labelNo;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
        }

        public void Yes()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        public void No()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            base.Close();
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
            System.Action onAttract)
        {
            ShowAttract(costType, cost.ToString(), content, attractMessage, onAttract);
        }

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
