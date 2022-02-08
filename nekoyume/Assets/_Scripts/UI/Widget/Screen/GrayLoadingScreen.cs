using Nekoyume.L10n;
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

            text.text = L10nManager.Localize("UI_IN_MINING_A_BLOCK");
        }

        public void Show(string message, bool localize)
        {
            if (localize)
            {
                message = L10nManager.Localize(message);
            }
            text.text = message;

            base.Show();
        }
    }
}
