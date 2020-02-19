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
        public Button button;
        public Image effectImage;
        public TextMeshProUGUI toggledOffText;
        public TextMeshProUGUI toggledOnText;
        public string localizationKey;
        
        private IToggleListener _toggleListener;

        protected void Awake()
        {
            IsToggleable = true;

            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                _toggleListener?.OnToggle(this);
            }).AddTo(gameObject);

            if (!string.IsNullOrEmpty(localizationKey))
            {
                string localization = LocalizationManager.Localize(localizationKey);
                toggledOffText.text = localization;
                toggledOnText.text = localization;
            }
        }
        
        #region IToggleable

        public string Name => name;
        public bool IsToggleable { get; set; }
        public bool IsToggledOn => effectImage.enabled;

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            button.interactable = false;
            effectImage.enabled = true;
            toggledOffText.enabled = false;
            toggledOnText.enabled = true;
        }
        
        public void SetToggledOff()
        {
            button.interactable = true;
            effectImage.enabled = false;
            toggledOffText.enabled = true;
            toggledOnText.enabled = false;
        }

        #endregion
    }
}
