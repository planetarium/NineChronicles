using System.Collections;
using System.Collections.Generic;
using Nekoyume.ApiClient;
using UnityEngine;
using TMPro;

namespace Nekoyume.UI
{
    public class SeasonPassCouragePopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI arena;

        [SerializeField]
        private TextMeshProUGUI worldboss;

        [SerializeField]
        private TextMeshProUGUI advanture;

        public override void Show(bool ignoreShowAnimation = false)
        {
            arena.text = $"+{ApiClients.Instance.SeasonPassServiceManager.ArenaCourageAmount}";
            worldboss.text = $"+{ApiClients.Instance.SeasonPassServiceManager.WorldBossCourageAmount}";
            advanture.text = $"+{ApiClients.Instance.SeasonPassServiceManager.AdventureCourageAmount}";
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Find<SeasonPassPremiumPopup>().Show();
        }
    }
}
