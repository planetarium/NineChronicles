using Assets.SimpleLocalization;
using System;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NotifiableButton : NormalButton, IWidgetControllable, IToggleable
    {
        public class Model : IDisposable
        {
            public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>();

            public void Dispose()
            {
                HasNotification.Dispose();
            }
        }

        public TextMeshProUGUI selectedText;
        public Image selectedImage;
        public Image hasNotificationImage;
        
        private IToggleListener _toggleListener;

        public readonly Model SharedModel = new Model();

        #region Mono

        protected override void Awake() 
        {
            base.Awake();
            selectedText.text = LocalizationManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            SharedModel.HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);

            button.OnClickAsObservable().Subscribe(_ => _toggleListener?.OnToggle(this))
                .AddTo(gameObject);
        }

        protected void OnDestroy()
        {
            SharedModel.Dispose();
        }

        #endregion

        #region IWidgetControllable

        private Widget _widget;
        private IDisposable _disposableForWidgetControllable;

        public bool HasWidget => !(_widget is null);
        
        public void SetWidgetType<T>() where T : Widget
        {
            _widget = Widget.Find<T>();
        }

        public void ShowWidget()
        {
            if (_widget is null)
                return;
            
            _widget.Show();
            _disposableForWidgetControllable = _widget.OnDisableSubject.Subscribe(_ => _toggleListener?.RequestToggledOff(this));
        }

        public void HideWidget()
        {
            if (_widget is null)
                return;

            _disposableForWidgetControllable?.Dispose();
            _widget.Close(true);
        }
        
        #endregion

        #region IToggleable
        
        public string Name => name;

        public bool IsToggledOn => _widget is null ? false : _widget.gameObject.activeSelf;

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            image.gameObject.SetActive(false);
            selectedImage.gameObject.SetActive(true);
            button.targetGraphic = selectedImage;

            ShowWidget();
        }

        public void SetToggledOff()
        {
            image.gameObject.SetActive(true);
            selectedImage.gameObject.SetActive(false);
            button.targetGraphic = image;

            HideWidget();
        }
        
        #endregion
    }
}
