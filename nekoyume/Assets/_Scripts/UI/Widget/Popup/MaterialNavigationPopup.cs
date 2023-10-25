using System;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
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
        private SubItemCount subItemCount;

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
            subItemCount.container.SetActive(false);
            blockGauge.container.SetActive(false);
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

        private readonly (int min, int max) _blockRange = new(0, 1700);
        public void ShowAP()
        {
            subItemCount.container.SetActive(true);
            subItemCount.icon.sprite = default;
            subItemCount.nameText.text = $"{default} :";
            subItemCount.countText.text = default;

            blockGauge.minText.text = _blockRange.min.ToString();
            blockGauge.maxText.text = _blockRange.max.ToString();

            int block = default;
            long remainBlockRange = _blockRange.max - block;
            blockGauge.container.SetActive(true);
            blockGauge.gaugeFillImage.fillAmount = (float)block/_blockRange.max;
            blockGauge.filledEffectImage.SetActive(block >= _blockRange.max);
            blockGauge.remainBlockText.text =
                $"{remainBlockRange:#,0}({remainBlockRange.BlockRangeToTimeSpanString()})";

            base.Show();
        }
    }
}
