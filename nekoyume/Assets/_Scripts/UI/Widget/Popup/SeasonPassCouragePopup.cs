using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Nekoyume.UI
{
    public class SeasonPassCouragePopup: PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI arena;
        [SerializeField]
        private TextMeshProUGUI worldboss;
        [SerializeField]
        private TextMeshProUGUI advanture;

        public override void Show(bool ignoreShowAnimation = false)
        {
            arena.text = $"+{Game.Game.instance.SeasonPassServiceManager.ArenaCourageAmount}";
            worldboss.text = $"+{Game.Game.instance.SeasonPassServiceManager.WorldBossCourageAmount}";
            advanture.text = $"+{Game.Game.instance.SeasonPassServiceManager.AdventureCourageAmount}";
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Widget.Find<SeasonPassPremiumPopup>().Show();
        }
    }
}
