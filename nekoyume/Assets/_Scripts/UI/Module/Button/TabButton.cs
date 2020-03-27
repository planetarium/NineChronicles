using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Game.Controller;
using Assets.SimpleLocalization;

namespace Nekoyume.UI.Module
{
    public class TabButton : MonoBehaviour, IToggleable
    {
        public Button button;
        public GameObject disabledContent;
        public Image disabledImage;
        public GameObject enabledContent;
        public Image enabledImage;

        public TextMeshProUGUI enabledText;
        public TextMeshProUGUI disabledText;
        public string localizationKey;

        #region IToggleable

        private IToggleListener _toggleListener;

        public string Name => name;

        public bool Toggleable { get; set; }

        public virtual bool IsToggledOn => enabledContent.activeSelf;

        public void SetToggledOff()
        {
            disabledContent.SetActive(true);
            enabledContent.SetActive(false);
        }

        public void SetToggledOn()
        {
            disabledContent.SetActive(false);
            enabledContent.SetActive(true);
        }

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        #endregion

        private void Awake()
        {
            button.onClick.AddListener(SubscribeOnClick);
            enabledText.text = LocalizationManager.Localize(localizationKey);
            disabledText.text = LocalizationManager.Localize(localizationKey);
        }

        private void SubscribeOnClick()
        {
            if (IsToggledOn)
                return;

            AudioController.PlayClick();
            _toggleListener?.OnToggle(this);
        }
    }
}
