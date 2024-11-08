using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Nekoyume.ApiClient;

namespace Nekoyume.UI.Module.Lobby
{
    using Nekoyume.L10n;
    using UniRx;

    public class SeasonPassMenu : MainMenu
    {
        [SerializeField]
        private GameObject premiumIcon;

        [SerializeField]
        private GameObject premiumPlusIcon;

        [SerializeField]
        private TextMeshProUGUI[] nameText;

        [SerializeField]
        private TextMeshProUGUI timeText;

        [SerializeField]
        private GameObject notificationObj;

        [SerializeField]
        private GameObject dim;

        [SerializeField]
        private GameObject iconRoot;

        public System.Action OnUserValueChanged;

        protected override void Awake()
        {
            base.Awake();

            dim.SetActive(false);
            iconRoot.SetActive(true);

            premiumIcon.SetActive(false);
            premiumPlusIcon.SetActive(false);

            OnUserValueChanged = () =>
            {
                int claimCount = ApiClients.Instance.SeasonPassServiceManager.HasClaimPassType.Count + ApiClients.Instance.SeasonPassServiceManager.HasPrevClaimPassType.Count;
                notificationObj.SetActive(claimCount > 0);
            };
            ApiClients.Instance.SeasonPassServiceManager.UpdatedUserDatas += OnUserValueChanged;

            foreach (var text in nameText)
            {
                text.text = L10nManager.Localize("SEASON_PASS_MENU_NAME");
            }
            ApiClients.Instance.SeasonPassServiceManager.RemainingDateTime.Subscribe((endDate) => { timeText.text = $"<Style=Clock> {endDate}"; });
        }

        protected void OnDestroy()
        {
            ApiClients.Instance.SeasonPassServiceManager.UpdatedUserDatas -= OnUserValueChanged;
        }
    }
}
