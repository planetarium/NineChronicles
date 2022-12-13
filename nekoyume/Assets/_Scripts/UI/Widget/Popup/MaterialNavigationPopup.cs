using Coffee.UIEffects;
using Nekoyume.Battle;
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
        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button actionButton;

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI itemCountText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI infoText;

        [SerializeField]
        private GameObject infoContainer;

        [SerializeField]
        private TextMeshProUGUI actionButtonText;

        [SerializeField]
        private UIHsvModifier hsvModifier;

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
            itemCountText.text = string.Format(L10nManager.Localize("UI_COUNT_FORMAT"), split[0]);
            contentText.text = content;
            actionButtonText.text = buttonText;
            infoText.gameObject.SetActive(infoText.text != string.Empty);
            infoContainer.SetActive(false);
            base.Show();
        }

        public void SetInfo(bool isActive, (string, bool) value = default)
        {
            infoContainer.SetActive(isActive);
            var (info, isPositive) = value;
            infoText.text = info;
            infoText.color = isPositive
                ? Palette.GetColor(ColorType.TextPositive)
                : Palette.GetColor(ColorType.TextDenial);
            hsvModifier.enabled = isPositive;
        }
    }
}
