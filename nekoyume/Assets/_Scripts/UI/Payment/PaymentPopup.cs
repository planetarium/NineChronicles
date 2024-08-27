using Nekoyume.L10n;
using System.Numerics;
using JetBrains.Annotations;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PaymentPopup : PopupWidget
    {
        private enum PopupType
        {
            AttractAction, // 재화부족 팝업에서 재화를 얻을 수 있는 경우
            PaymentCheck, // 재화 소모 확인을 요청하는 경우
            NoneAction, // 재화부족 팝업에서 재화를 얻을 수 없는 경우
        }

        // TODO: CostType과 동일하게 쓸 수 있을지는 확인
        public enum PaymentType
        {
            Crystal,
            SilverDust,
            GoldenDust, // TODO: GoldDust로 쓰는곳도 있고, GoldenDust로 쓰는 곳도 있어 정리가 안된듯함
            RubyDust,
            EmeraldDust,
            NCG,
            NCGStaking,
            MonsterCollection,
            RuneStoneSummonOnly,
            RuneStone,
            ActionPoint,
            APPortion,
        }

#region SerializeField
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

        [SerializeField]
        private GameObject attractArrowObject;
        
        [SerializeField]
        private TextMeshProUGUI titleText;
        
        [SerializeField]
        private TextMeshProUGUI contentText;
        
        [SerializeField]
        private TextButton buttonYes;
        
        [SerializeField]
        private TextButton buttonNo;
        
        [SerializeField]
        private Button buttonClose;
        
        [SerializeField]
        private GameObject titleBorder;
#endregion SerializeField

        private ConfirmDelegate CloseCallback { get; set; }
        
        protected override void Awake()
        {
            base.Awake();

            buttonNo.OnClick = No;
            buttonYes.OnClick = Yes;
            buttonClose.onClick.AddListener(NoWithoutCallback);
            CloseWidget = NoWithoutCallback;
            SubmitWidget = Yes;
        }
        
        private void SetPopupType(PopupType popupType)
        {
            switch (popupType)
            {
                case PopupType.AttractAction:
                    buttonYes.gameObject.SetActive(true);
                    buttonNo.gameObject.SetActive(false);
                    buttonClose.gameObject.SetActive(true);
                    attractArrowObject.SetActive(true);
                    break;
                case PopupType.PaymentCheck:
                    buttonYes.gameObject.SetActive(true);
                    buttonNo.gameObject.SetActive(true);
                    buttonClose.gameObject.SetActive(false);
                    attractArrowObject.SetActive(false);
                    break;
                case PopupType.NoneAction:
                    buttonYes.gameObject.SetActive(false);
                    buttonNo.gameObject.SetActive(false);
                    buttonClose.gameObject.SetActive(true);
                    attractArrowObject.SetActive(false);
                    break;
            }
        }

#region LackPaymentAction
        public void ShowLackPayment(
            CostType costType,
            string cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            SetPopupType(PopupType.AttractAction);
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

        public void ShowLackRuneStone(
            RuneItem runeItem,
            string requiredCost)
        {
            var runeStone = runeItem.RuneStone;
            var hasAttract = HasAttractActionRuneStone(runeStone);
            SetPopupType(hasAttract ? PopupType.AttractAction : PopupType.NoneAction);
            
            costIcon.overrideSprite = GetRuneStoneSprite(runeStone);
            costText.text = requiredCost;
            
            var title = L10nManager.Localize("UI_REQUIRED_COST");
            var content = GetRuneStoneContent(runeStone);
            var attractMessage = L10nManager.Localize("UI_SUMMON");
            
            CloseCallback = result =>
            {
            };
            Show(title, content, attractMessage);
        }

        public void ShowLackPayment(
            CostType costType,
            BigInteger cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            ShowLackPayment(costType, cost.ToString(), content, attractMessage, onAttract);
        }
#endregion LackPaymentAction
        
#region PaymentCheckAction
        public void ShowCheckPayment(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string enoughMessage,
            string insufficientMessage,
            System.Action onPaymentSucceed,
            System.Action onAttract)
        {
            SetPopupType(PopupType.PaymentCheck);
            addCostContainer.SetActive(false);
            
            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(costType);

            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            CloseCallback = result =>
            {
                if (result != ConfirmResult.Yes)
                {
                    return;
                }

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
                    ShowLackPayment(costType, cost, insufficientMessage, attractMessage, onAttract);
                }
            };
            
            SetContent(popupTitle, enoughMessage, yes, no, false);
            Show(popupTitle, enoughMessage, yes, no, false);
        }
#endregion PaymentCheckAction

#region General
        private void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL", bool localize = true)
        {
            SetContent(title, content, labelYes, labelNo, localize);

            if (!gameObject.activeSelf)
            {
                Show();
            }
        }
        
        private void SetContent(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            var titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                {
                    titleText.text = L10nManager.Localize(title);
                }

                contentText.text = L10nManager.Localize(content);
                buttonYes.Text = L10nManager.Localize(labelYes);
                buttonNo.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                titleText.text = title;
                contentText.text = content;
                buttonYes.Text = labelYes;
                buttonNo.Text = labelNo;
            }

            titleText.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
        }
#endregion General

#region Helper
        private CostType GetCostType(PaymentType paymentType)
        {
            switch (paymentType)
            {
                case PaymentType.Crystal:
                    return CostType.Crystal;
                case PaymentType.SilverDust:
                    return CostType.SilverDust;
                case PaymentType.GoldenDust:
                    return CostType.GoldDust;
                case PaymentType.RubyDust:
                    return CostType.RubyDust;
                case PaymentType.EmeraldDust:
                    return CostType.EmeraldDust;
                case PaymentType.NCG:
                    return CostType.NCG;
                case PaymentType.NCGStaking:
                    return CostType.NCG;
                case PaymentType.ActionPoint:
                    return CostType.ActionPoint;
                case PaymentType.APPortion:
                    return CostType.ApPotion;
                default:
                    return CostType.None;
            }
        }
        
        [CanBeNull]
        private Sprite GetCostIcon(PaymentType paymentType)
        {
            var costType = GetCostType(paymentType);
            return costType != CostType.None ? costIconData.GetIcon(costType) : null;
        }

#region RuneStoneHelper
        /// <summary>
        /// 해당 runeStone을 획득할 AttractAction이 있는지 확인합니다.
        /// </summary>
        /// <param name="runeStone">NoneAction인지 확인할 runeStone의 fav 값</param>
        /// <returns>PC Shop에서 거래 가능하지만, 모바일인 경우 false</returns>
        private bool HasAttractActionRuneStone(FungibleAssetValue runeStone)
        {
            if (runeStone.IsTradable())
            {
                // 에디터 안드로이드, IOS 테스트를 위해 RUN_ON_MOBILE을 사용하지 않음
#if UNITY_ANDROID || UNITY_IOS
                return false;
#endif
            }

            return true;
        }

        private string GetRuneStoneContent(FungibleAssetValue runeStone)
        {
            var hasAttract = HasAttractActionRuneStone(runeStone);
            if (runeStone.IsTradable())
            {
                return L10nManager.Localize(hasAttract ?
                    "UI_MESSAGE_NOT_ENOUGH_TRADEABLE_RUNESTONE_PC" :
                    "UI_MESSAGE_NOT_ENOUGH_TRADEABLE_RUNESTONE_MOBILE");
            }
            return L10nManager.Localize("UI_MESSAGE_NOT_ENOUGH_UNTRADEABLE_RUNESTONE");
        }
        
        private Sprite GetRuneStoneSprite(FungibleAssetValue runeStone)
        {
            return RuneFrontHelper.TryGetRuneStoneIcon(runeStone.Currency.Ticker, out var icon) ? icon : null;
        }
#endregion RuneStoneHelper
#endregion Helper

        private void Yes()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        private void No()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            base.Close();
        }

        // TODO: Remove after add world boss exception
        public void ShowAttractWithAddCost(
            string title,
            string content,
            CostType costType,
            int cost,
            CostType addCostType,
            int addCost,
            System.Action onConfirm)
        {
            SetPopupType(PopupType.AttractAction);
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
    }
}
