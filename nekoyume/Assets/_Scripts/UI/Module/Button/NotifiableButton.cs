using System;
using System.Collections.Generic;
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

        public Image hasNotificationImage;
        
        private Widget _widget;
        
        private readonly List<IToggleListener> _toggleListeners = new List<IToggleListener>();

        public readonly Model SharedModel = new Model();

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            SharedModel.HasNotification.SubscribeToBehaviour(hasNotificationImage).AddTo(gameObject);
            
            button.OnClickAsObservable().Subscribe(_ =>
            {
                foreach (var toggleListener in _toggleListeners)
                {
                    toggleListener.OnToggled(this);
                }
            }).AddTo(gameObject);
        }

        protected void OnDestroy()
        {
            SharedModel.Dispose();
        }

        #endregion

        #region IWidgetControllable
        
        public void SetWidgetType<T>() where T : Widget
        {
            _widget = Widget.Find<T>();
        }

        public void ShowWidget()
        {
            if (_widget is null)
                return;
            
            _widget.Show();
        }

        public void HideWidget()
        {
            if (_widget is null)
                return;
            
            _widget.Close();
        }
        
        #endregion

        #region IToggleable

        public bool IsToggledOn => _widget is null ? false : _widget.gameObject.activeSelf;

        public void RegisterToggleListener(IToggleListener toggleListener)
        {
            _toggleListeners.Add(toggleListener);
        }

        public void SetToggledOn()
        {
            // Do nothing.
        }

        public void SetToggledOff()
        {
            // Do nothing.
        }
        
        #endregion
    }
}
