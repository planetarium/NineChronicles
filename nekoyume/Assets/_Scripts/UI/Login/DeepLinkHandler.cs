using Libplanet;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DeepLinkHandler
    {
        private System.Action _onPortalEnd;
        private string _deeplinkURL;

        private string _portalUrl;

        public DeepLinkHandler(string url)
        {
            _portalUrl = url;

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

            if (_onPortalEnd != null)
            {
                _onPortalEnd();
                _onPortalEnd = null;
            }
        }

        public void OpenPortal(Address avatarAddress, System.Action onPortalEnd = null)
        {
            _onPortalEnd = onPortalEnd;

            var url = $"{_portalUrl}?step=2&address={avatarAddress}";
            Application.OpenURL(url);
        }
    }
}
