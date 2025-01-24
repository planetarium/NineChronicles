using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DimmedLoadingScreen : ScreenWidget
    {
        public enum ContentType
        {
            Undefined = 0,
            ConnectingToTheNetworks,
            WaitingForSocialAuthenticating,
            WaitingForPortalAuthenticating,
            WaitingForPlanetAccountInfoSyncing,
            WaitingForConnectingToPlanet,
        }

        public override WidgetType WidgetType => WidgetType.System;

        [SerializeField]
        private TMP_Text titleText;

        private ContentType _contentType = ContentType.Undefined;

        public void Show(
            ContentType contentType = ContentType.ConnectingToTheNetworks,
            bool ignoreShowAnimation = false)
        {
            UpdateContent(contentType);
            if (IsActive())
            {
                return;
            }

            Show(ignoreShowAnimation);
        }

        private void UpdateContent(ContentType contentType)
        {
            if (contentType == _contentType)
            {
                return;
            }

            switch (contentType)
            {
                case ContentType.Undefined:
                    break;
                case ContentType.ConnectingToTheNetworks:
                    titleText.text = L10nManager.Localize("LOADING_TO_CONNECT_TO_NETWORKS");
                    break;
                case ContentType.WaitingForSocialAuthenticating:
                    titleText.text = L10nManager.Localize("LOADING_FOR_SOCIAL_AUTHENTICATION");
                    break;
                case ContentType.WaitingForPortalAuthenticating:
                    titleText.text = L10nManager.Localize("LOADING_FOR_PORTAL_AUTHENTICATION");
                    break;
                case ContentType.WaitingForPlanetAccountInfoSyncing:
                    titleText.text = L10nManager.Localize("LOADING_TO_SYNCHRONIZE_PLANET_ACCOUNT_INFORMATION");
                    break;
                case ContentType.WaitingForConnectingToPlanet:
                    titleText.text = L10nManager.Localize("LOADING_TO_CONNECT_TO_PLANET");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentType), contentType, null);
            }

            _contentType = contentType;
        }
    }
}
