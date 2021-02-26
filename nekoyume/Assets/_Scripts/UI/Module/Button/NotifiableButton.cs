using System;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NotifiableButton : ToggleableButton
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

        public readonly Model SharedModel = new Model();

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            SharedModel.HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);
        }

        protected void OnDestroy()
        {
            SharedModel.Dispose();
        }

        #endregion
    }
}
