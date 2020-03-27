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
        private Button button = null;

        [SerializeField]
        private Image normalImage = null;

        [SerializeField]
        private Image selectedImage = null;

        [SerializeField]
        private TextMeshProUGUI normalText = null;

        [SerializeField]
        private TextMeshProUGUI selectedText = null;

        [SerializeField]
        private TextMeshProUGUI disabledText = null;

        [SerializeField]
        private string localizationKey = null;


        private IToggleListener _toggleListener;

        public readonly Subject<CategoryButton> OnClick = new Subject<CategoryButton>();

        protected void Awake()
        {
            Toggleable = true;

            if (!string.IsNullOrEmpty(localizationKey))
            {
                var localization = LocalizationManager.Localize(localizationKey);
                normalText.text = localization;
                selectedText.text = localization;
            }

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
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
            if (!Toggleable)
                return;

            selectedImage.enabled = true;
            normalText.enabled = false;
            selectedText.enabled = true;
            disabledText.enabled = false;
        }

        public void SetToggledOff()
        {
            if (!Toggleable)
                return;

            selectedImage.enabled = false;
            normalText.enabled = true;
            selectedText.enabled = false;
            disabledText.enabled = false;
        }

        #endregion

        public void SetInteractable(bool interactable, bool ignoreImageColor = false)
        {
            button.interactable = interactable;

            if (ignoreImageColor)
            {
                return;
            }

            var imageColor = button.interactable
                ? Color.white
                : Color.gray;
            normalImage.color = imageColor;
            selectedImage.color = imageColor;
        }

        private void SubscribeOnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClick.OnNext(this);
            _toggleListener?.OnToggle(this);
        }
    }
}
