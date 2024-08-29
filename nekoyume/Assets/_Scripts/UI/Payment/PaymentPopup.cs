using System;
using Nekoyume.L10n;
using System.Numerics;
using JetBrains.Annotations;
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
        private const string MonsterCollectionString = "MONSTER COLLECTION";
        private const string AdventureBossString = "ADVENTURE BOSS";

        private enum PopupType
        {
            AttractAction, // 재화부족 팝업에서 재화를 얻을 수 있는 경우
            PaymentCheck, // 재화 소모 확인을 요청하는 경우
            NoneAction, // 재화부족 팝업에서 재화를 얻을 수 없는 경우
        }

        // TODO: 현재 추가해야하는 재화 타입을 확인하기 위해 메모용으로 임시 추가
        // TODO: 가능하면 CostType을 사용할 것이지만, 대체 불가능하다 판단되면 이 타입을 사용할 것
        // public enum PaymentType
        // {
        //     Crystal,
        //     SilverDust,
        //     GoldenDust, // TODO: GoldDust로 쓰는곳도 있고, GoldenDust로 쓰는 곳도 있어 정리가 안된듯함
        //     RubyDust,
        //     EmeraldDust,
        //     NCG,
        //     NCGStaking,
        //     MonsterCollection,
        //     RuneStoneSummonOnly, // 룬조각?
        //     RuneStone,
        //     ActionPoint,
        //     APPortion,
        // }

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

        public void ShowLackPayment(
            CostType costType,
            BigInteger cost,
            string content,
            string attractMessage,
            System.Action onAttract)
        {
            ShowLackPayment(costType, cost.ToString(), content, attractMessage, onAttract);
        }

        // TODO: 해당 메서드 기반으로 위의 ShowLackPayment을 대체할 수 있을 것으로 보인다면,
        // TODO: 위의 ShowLackPayment를 Obsolete 처리 후 아래의 메서드를 사용하도록 변경
        public void ShowLackPaymentDust(CostType costType, BigInteger cost)
        {
            // TODO: remove
            addCostContainer.SetActive(false);

            var canAttract = CanAttractDust(costType);
            SetPopupType(canAttract ? PopupType.AttractAction : PopupType.NoneAction);

            costIcon.overrideSprite = costIconData.GetIcon(costType);
            var title = L10nManager.Localize("UI_REQUIRED_COUNT");
            costText.text = cost.ToString();
            var content = GetLackDustContentString(costType);

            CloseCallback = result =>
            {
                if (canAttract && result == ConfirmResult.Yes)
                {
                    AttractDust(costType);
                }
            };
            Show(title, content, GetDustAttractString(costType), string.Empty, false);
        }
        
        public void ShowLackPaymentCrystal(BigInteger cost)
        {
            // TODO: remove
            addCostContainer.SetActive(false);
            SetPopupType(PopupType.AttractAction);
            
            costIcon.overrideSprite = costIconData.GetIcon(CostType.Crystal);
            var title = L10nManager.Localize("UI_REQUIRED_COUNT");
            costText.text = cost.ToString();
            var content = L10nManager.Localize("UI_LACK_CRYSTAL");
            var labelYesText = L10nManager.Localize("GRIND_UI_BUTTON");
            
            CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    AttractGrind();
                }
            };
            Show(title, content, labelYesText, string.Empty, false);
        }
#endregion LackPaymentAction

#region PaymentCheckAction
        public void ShowCheckPayment(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
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

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        // TODO: 재화 관리 팝업관련 작업을 진행하며 정리되면 제거
        public void ShowCheckPaymentDust(
            CostType costType,
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            System.Action onPaymentSucceed)
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
                    ShowLackPaymentDust(costType, cost);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }

        public void ShowCheckPaymentCrystal(
            BigInteger balance,
            BigInteger cost,
            string checkCostMessage,
            System.Action onPaymentSucceed)
        {
            SetPopupType(PopupType.PaymentCheck);
            addCostContainer.SetActive(false);

            var popupTitle = L10nManager.Localize("UI_TOTAL_COST");
            var enoughBalance = balance >= cost;
            costText.text = cost.ToString();
            costIcon.overrideSprite = costIconData.GetIcon(CostType.Crystal);

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
                    ShowLackPaymentCrystal(cost);
                }
            };

            SetContent(popupTitle, checkCostMessage, yes, no, false);
            Show(popupTitle, checkCostMessage, yes, no, false);
        }
#endregion PaymentCheckAction

// TODO: RuneHelper등과 통합? 혹은 별도 파일(PaymentHelper등)으로 분리
#region DustHelper
        public static string GetLackDustContentString(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                    return L10nManager.Localize("UI_LACK_SILVER_DUST");
                case CostType.GoldDust:
                    return L10nManager.Localize("UI_LACK_GOLD_DUST");
                case CostType.RubyDust:
                    return L10nManager.Localize("UI_LACK_RUBY_DUST");
                case CostType.EmeraldDust:
                    return L10nManager.Localize(CanAttractDust(costType) ?
                        "UI_LACK_EMERALD_DUST" :
                        "UI_LACK_EMERALD_DUST_KR");
            }

            return string.Empty;
        }

        // TODO: 모든 CostType에 대해 처리 가능하면 메서드명 일반적이게 변경하고 수정
        private string GetDustAttractString(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    return MonsterCollectionString;
                case CostType.EmeraldDust:
                    return AdventureBossString;
            }

            return string.Empty;
        }

        private static bool CanAttractDust(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    return true;
                case CostType.EmeraldDust:
                    return !Game.LiveAsset.GameConfig.IsKoreanBuild;
            }

            return false;
        }

        private void AttractDust(CostType costType)
        {
            switch (costType)
            {
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    AttractToMonsterCollection();
                    return;
                case CostType.EmeraldDust:
                    AttractToAdventureBoss();
                    return;
            }

            NcDebug.LogWarning($"[{nameof(PaymentPopup)}] AttractDust: Invalid costType.");
        }
#endregion DustHelper

#region Attract
        private void AttractToMonsterCollection()
        {
            // 기능 충돌 방지를 위해 StakingPopup실행 전 다른 UI를 닫음
            CloseWithOtherWidgets();
            Game.Event.OnRoomEnter.Invoke(true);
            Find<StakingPopup>().Show();
        }

        private void AttractToAdventureBoss()
        {
            CloseWithOtherWidgets();

            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                // K빌드인 경우 로비로 이동
                NcDebug.LogWarning("Korean build is not supported.");
                Game.Event.OnRoomEnter.Invoke(true);
                return;
            }

            Find<WorldMap>().Show();

            var currState = Game.Game.instance.AdventureBossData.CurrentState.Value;
            if (currState == AdventureBossData.AdventureBossSeasonState.Progress)
            {
                WorldMapAdventureBoss.OnClickOpenAdventureBoss();
            }
        }
        
        private void AttractGrind()
        {
            Find<Menu>().Close();
            Find<WorldMap>().Close();
            Find<StageInformation>().Close();
            Find<BattlePreparation>().Close();
            Find<Craft>().Close(true);
            Find<Grind>().Show();
        }
#endregion Attract

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
