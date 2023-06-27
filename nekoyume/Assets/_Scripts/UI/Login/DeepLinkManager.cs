using Libplanet;
using Nekoyume.Model.Mail;
using Nekoyume.Pattern;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DeepLinkManager : MonoSingleton<DeepLinkManager>
    {
        private System.Action _onPortalEnd;
        private string _deeplinkURL;

        private const string PortalUrlFormat =
            // "https://nine-chronicles.com/start?step=2&address={0}";
            "https://feat-mead.d3mkl7om8mgjhs.amplifyapp.com/start?step=2&address={0}";

        protected override void Awake()
        {
            base.Awake();

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
            else _deeplinkURL = "[none]";
        }

        private void OnDeepLinkActivated(string url)
        {
            _deeplinkURL = url;
            Debug.Log("DeepLinkManager - Received deep link: " + _deeplinkURL);

            if (_onPortalEnd != null)
            {
                _onPortalEnd();
                _onPortalEnd = null;
            }

            NotificationSystem.Push(MailType.System, "DeepLinkManager - Received deep link: " + _deeplinkURL, NotificationCell.NotificationType.Information);
        }

        public void OpenPortal(Address avatarAddress, System.Action onPortalEnd)
        {
            _onPortalEnd = onPortalEnd;

            var url = string.Format(PortalUrlFormat, avatarAddress);
            Application.OpenURL(url);
        }
    }
}
