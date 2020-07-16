using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class GrayLoadingScreen : ScreenWidget
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        protected override void Awake()
        {
            base.Awake();

            text.text = LocalizationManager.Localize("UI_IN_MINING_A_BLOCK");
        }
    }
}
