using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class TabButton : MonoBehaviour, IToggleable
    {
        [SerializeField]
        protected Button button = null;

        [SerializeField]
        protected GameObject disabledContent = null;

        [SerializeField]
        protected Image disabledImage = null;

        [SerializeField]
        protected GameObject enabledContent = null;

        [SerializeField]
        protected Image enabledImage = null;

        [SerializeField]
        protected Image hasNotificationImage = null;

        [SerializeField]
        protected TextMeshProUGUI enabledText = null;

        [SerializeField]
        protected TextMeshProUGUI disabledText = null;

        [SerializeField]
        protected string localizationKey = null;

        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

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
            HasNotification.SubscribeTo(hasNotificationImage)
                .AddTo(gameObject);
            if (!string.IsNullOrEmpty(localizationKey))
            {
                enabledText.text = L10nManager.Localize(localizationKey);
                disabledText.text = L10nManager.Localize(localizationKey);
            }
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
