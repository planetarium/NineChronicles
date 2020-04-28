using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ToggleableButton : MonoBehaviour, IToggleable, IWidgetControllable
    {
        [SerializeField] public Button button = null;
        [SerializeField] public TextMeshProUGUI toggledOffText = null;
        [SerializeField] public Image toggledOffImage = null;
        [SerializeField] protected TextMeshProUGUI toggledOnText = null;
        [SerializeField] protected Image toggledOnImage = null;
        [SerializeField] protected string localizationKey = null;

        private IToggleListener _toggleListener;

        public readonly Subject<ToggleableButton> OnClick = new Subject<ToggleableButton>();

        #region Mono

        protected virtual void Awake()
        {
            Toggleable = true;
            IsWidgetControllable = true;

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);

            if (!string.IsNullOrEmpty(localizationKey))
            {
                var text = LocalizationManager.Localize(localizationKey);
                toggledOffText.text = text;
                toggledOnText.text = text;
            }
        }

        #endregion

        #region IWidgetControllable

        private Widget _widget;
        private IDisposable _disposableForWidgetControllable;

        public bool IsWidgetControllable { get; set; }
        public bool HasWidget => !(_widget is null);

        public void SetWidgetType<T>() where T : Widget
        {
            _widget = Widget.Find<T>();
        }

        public virtual void ShowWidget()
        {
            if (_widget is null || !IsWidgetControllable)
                return;

            _widget.Show();
            _disposableForWidgetControllable = _widget.OnDisableObservable.Subscribe(_ => _toggleListener?.RequestToggledOff(this));
        }

        public virtual void HideWidget()
        {
            if (_widget is null || !IsWidgetControllable)
                return;

            _disposableForWidgetControllable?.Dispose();

            if (!_widget.IsActive())
                return;

            if (_widget is Confirm confirm)
            {
                confirm.NoWithoutCallback();
            }
            if (_widget is InputBox inputBox)
            {
                inputBox.No();
            }
            else
                _widget.Close();
        }

        #endregion

        #region IToggleable

        public string Name => name;

        public bool Toggleable { get; set; }

        public virtual bool IsToggledOn => toggledOnImage.gameObject.activeSelf;

        public virtual void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public virtual void SetToggledOn()
        {
            if (!Toggleable)
                return;

            toggledOffImage.gameObject.SetActive(false);
            toggledOnImage.gameObject.SetActive(true);
            button.targetGraphic = toggledOnImage;

            ShowWidget();
        }

        public virtual void SetToggledOff()
        {
            if (!Toggleable)
                return;

            toggledOffImage.gameObject.SetActive(true);
            toggledOnImage.gameObject.SetActive(false);
            button.targetGraphic = toggledOffImage;

            HideWidget();
        }

        #endregion

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

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
            toggledOffImage.color = imageColor;
            toggledOnImage.color = imageColor;
        }

        private void SubscribeOnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClick.OnNext(this);
            _toggleListener?.OnToggle(this);
        }
    }
}
