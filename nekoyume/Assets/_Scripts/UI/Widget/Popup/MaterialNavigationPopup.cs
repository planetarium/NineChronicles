using System;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MaterialNavigationPopup : PopupWidget
    {
        [Serializable]
        private struct SubItemCount
        {
            public GameObject container;
            public Image icon;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI countText;
        }

        [Serializable]
        private struct BlockGauge
        {
            public GameObject container;
            public Image gaugeFillImage;
            public GameObject filledEffectImage;
            public TextMeshProUGUI minText;
            public TextMeshProUGUI maxText;
            public TextMeshProUGUI remainBlockText;
            public BonusItem bonusItem;
        }

        [Serializable]
        private struct BonusItem
        {
            public GameObject container;
            public Image icon;
            public TextMeshProUGUI countText;
            public Button button;
        }

        [Serializable]
        private struct InfoText
        {
            public GameObject container;
            public UIHsvModifier hsvModifier;
            public TextMeshProUGUI infoText;
        }

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI itemCountText;

        [SerializeField]
        private SubItemCount subItem;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private BlockGauge blockGauge;

        [SerializeField]
        private InfoText infoText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button actionButton;

        [SerializeField]
        private TextMeshProUGUI actionButtonText;

        [SerializeField]
        private ConditionalButton conditionalButtonBrown;

        [SerializeField]
        private ConditionalButton conditionalButtonYellow;

        private System.Action _callback;

        protected override void Awake()
        {
            base.Awake();

            confirmButton.onClick.AddListener(() => Close(true));
            actionButton.onClick.AddListener(() =>
            {
                Close(true);
                _callback?.Invoke();
            });
            conditionalButtonBrown.OnSubmitSubject.Subscribe(_ => InvokeAfterActionPointCheck(() =>
            {
                Close();
                _chargeAP?.Invoke();
            }));
            conditionalButtonYellow.OnSubmitSubject.Subscribe(_ => InvokeAfterActionPointCheck(() =>
            {
                Close();
                _getDailyReward?.Invoke();
            }));
        }

        private static void InvokeAfterActionPointCheck(System.Action action)
        {
            if (States.Instance.CurrentAvatarState.actionPoint > 0)
            {
                ActionPoint.ShowRefillConfirmPopup(action);
            }
            else
            {
                action();
            }
        }

        public void Show(
            System.Action callback,
            Sprite itemIcon,
            string itemName,
            string itemCount,
            string content,
            string buttonText)
        {
            _callback = callback;
            itemImage.sprite = itemIcon;
            itemNameText.text = itemName;
            var split = itemCount.Split('.');
            itemCountText.text = L10nManager.Localize("UI_COUNT_FORMAT", split[0]);
            contentText.text = content;
            actionButtonText.text = buttonText;
            infoText.infoText.gameObject.SetActive(infoText.infoText.text != string.Empty);
            infoText.container.SetActive(false);  // set default
            subItem.container.SetActive(false);
            blockGauge.container.SetActive(false);
            actionButton.gameObject.SetActive(true);
            conditionalButtonBrown.gameObject.SetActive(false);
            conditionalButtonYellow.gameObject.SetActive(false);
            base.Show();
        }

        public void SetInfo(bool isActive, (string, bool) value = default)
        {
            infoText.container.SetActive(isActive);
            var (info, isPositive) = value;
            infoText.infoText.text = info;
            infoText.infoText.color = isPositive
                ? Palette.GetColor(ColorType.TextPositive)
                : Palette.GetColor(ColorType.TextDenial);
            infoText.hsvModifier.enabled = isPositive;
        }

        private const string ActionPointTicker = "ACTIONPOINT";
        private const int ActionPointItemId = 9999996;
        private const string AdventureRuneTicker = "RUNE_ADVENTURE";
        private const int ItemId = 500000;

        private System.Action _chargeAP;
        private System.Action _getDailyReward;

        public void ShowAP(
            string itemCount,
            int subItemCount,
            long blockRange,
            long maxBlockRange, //
            bool isInteractable,
            System.Action chargeAP, //
            System.Action getDailyReward,
            bool ignoreShowAnimation = false) //
        {
            itemImage.sprite = SpriteHelper.GetFavIcon(ActionPointTicker);
            itemNameText.text = L10nManager.Localize($"ITEM_NAME_{ActionPointItemId}");
            itemCountText.text = itemCount;
            contentText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{ActionPointItemId}");;
            infoText.container.SetActive(false); // set default

            actionButton.gameObject.SetActive(false);
            conditionalButtonBrown.gameObject.SetActive(true);
            conditionalButtonYellow.gameObject.SetActive(true);

            subItem.container.SetActive(true);
            subItem.icon.sprite = SpriteHelper.GetItemIcon(ItemId);
            subItem.nameText.text = $"{L10nManager.Localize($"ITEM_NAME_{ItemId}")} :";
            subItem.countText.text = subItemCount.ToString();

            blockGauge.minText.text = 0.ToString();
            blockGauge.maxText.text = maxBlockRange.ToString();
            blockGauge.bonusItem.icon.sprite = SpriteHelper.GetFavIcon(AdventureRuneTicker);
            blockGauge.bonusItem.countText.text = 1.ToString();
            blockGauge.bonusItem.button.onClick.RemoveAllListeners();  // Todo Fill this

            var remainBlockRange = maxBlockRange - blockRange;
            blockGauge.container.SetActive(true);
            blockGauge.gaugeFillImage.fillAmount = (float)blockRange / maxBlockRange;
            blockGauge.filledEffectImage.SetActive(blockRange >= maxBlockRange);
            blockGauge.remainBlockText.text =
                $"{remainBlockRange:#,0}({remainBlockRange.BlockRangeToTimeSpanString()})";

            conditionalButtonBrown.Interactable = isInteractable && subItemCount > 0;
            conditionalButtonYellow.Interactable = isInteractable && remainBlockRange <= 0;
            _chargeAP = chargeAP;
            _getDailyReward = getDailyReward;

            base.Show(ignoreShowAnimation);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionActionPointChargeButton()
        {
            Close(true);
            _getDailyReward();
        }
    }
}
