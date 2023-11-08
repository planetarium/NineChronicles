using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DimmedLoadingScreen : ScreenWidget
    {
        public enum Case
        {
            ConnectingToTheNetworks = 0,
            WaitingForSocialAuthenticating,
            WaitingForPortalAuthenticating,
            WaitingForPlanetAccountInfoSyncing,
        }

        public override WidgetType WidgetType => WidgetType.System;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text subText;

        public void Show(
            Case enumCase = Case.ConnectingToTheNetworks,
            bool ignoreShowAnimation = false)
        {
            UpdateContent(enumCase);
            if (IsActive())
            {
                return;
            }

            Show(ignoreShowAnimation);
        }

        private void UpdateContent(Case enumCase)
        {
            switch (enumCase)
            {
                case Case.ConnectingToTheNetworks:
                    titleText.text = L10nManager.Localize("LOADING_TO_CONNECT_TO_NETWORKS");
                    subText.text = L10nManager.Localize("LOADING_TO_CONNECT_TO_NETWORKS_DESC");
                    break;
                case Case.WaitingForSocialAuthenticating:
                    titleText.text = L10nManager.Localize("LOADING_FOR_SOCIAL_AUTHENTICATION");
                    subText.text = string.Empty;
                    break;
                case Case.WaitingForPortalAuthenticating:
                    titleText.text = L10nManager.Localize("LOADING_FOR_PORTAL_AUTHENTICATION");
                    subText.text = string.Empty;
                    break;
                case Case.WaitingForPlanetAccountInfoSyncing:
                    titleText.text = L10nManager.Localize("LOADING_TO_SYNCHRONIZE_PLANET_ACCOUNT_INFORMATION");
                    subText.text = string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(enumCase), enumCase, null);
            }
        }
    }
}
