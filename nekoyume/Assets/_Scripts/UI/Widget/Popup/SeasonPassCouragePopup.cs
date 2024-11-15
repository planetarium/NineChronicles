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
            var seasonPassServiceManager = ApiClients.Instance.SeasonPassServiceManager;
            arena.text = $"+{seasonPassServiceManager.ExpPointAmount(SeasonPassServiceClient.PassType.CouragePass, SeasonPassServiceClient.ActionType.battle_arena)}";
            worldboss.text = $"+{seasonPassServiceManager.ExpPointAmount(SeasonPassServiceClient.PassType.CouragePass, SeasonPassServiceClient.ActionType.raid)}";
            advanture.text = $"+{seasonPassServiceManager.ExpPointAmount(SeasonPassServiceClient.PassType.CouragePass, SeasonPassServiceClient.ActionType.hack_and_slash)}";
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Find<SeasonPassPremiumPopup>().Show();
        }
    }
}
