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
            _portalUrl = url ?? throw new System.ArgumentNullException(nameof(url));

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

            string os = null;
#if UNITY_ANDROID
            os = "android";
#endif
            var url = $"{_portalUrl}/start?step=2&address={avatarAddress}&os={os}";
            Application.OpenURL(url);
        }
    }
}
