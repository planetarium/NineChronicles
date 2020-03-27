using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CategoryButton : MonoBehaviour, IToggleable
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image selectedImage;

        [SerializeField]
        private TextMeshProUGUI normalText;

        [SerializeField]
        private TextMeshProUGUI selectedText;

        [SerializeField]
        private TextMeshProUGUI disabledText;

        [SerializeField]
        private string localizationKey;


        private IToggleListener _toggleListener;

        public readonly Subject<CategoryButton> OnClick = new Subject<CategoryButton>();

        protected void Awake()
        {
            Toggleable = true;

            if (!string.IsNullOrEmpty(localizationKey))
            {
                string localization = LocalizationManager.Localize(localizationKey);
                normalText.text = localization;
                selectedText.text = localization;
            }

            button.onClick.AddListener(SubscribeOnClick);
        }

        #region IToggleable

        public string Name => name;

        public bool Toggleable { get; set; }

        public bool IsToggledOn => selectedImage.enabled;

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            button.interactable = false;
            selectedImage.enabled = true;
            normalText.enabled = false;
            selectedText.enabled = true;
            disabledText.enabled = false;
        }

        public void SetToggledOff()
        {
            button.interactable = true;
            selectedImage.enabled = false;
            normalText.enabled = true;
            selectedText.enabled = false;
            disabledText.enabled = false;
        }

        #endregion

        private void SubscribeOnClick()
        {
            if (IsToggledOn)
                return;

            AudioController.PlayClick();
            OnClick.OnNext(this);
            _toggleListener?.OnToggle(this);
        }
    }
}
