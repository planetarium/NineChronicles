using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrayLoadingScreen : ScreenWidget
    {
        [SerializeField]
        private TextMeshProUGUI text;

        [SerializeField]
        private Image background;

        protected override void Awake()
        {
            base.Awake();

            text.text = L10nManager.Localize("UI_IN_MINING_A_BLOCK");
        }

        public void Show(string message, bool localize, float alpha = 0.4f)
        {
            if (localize)
            {
                message = L10nManager.Localize(message);
            }

            text.text = message;

            var color = background.color;
            color.a = alpha;
            background.color = color;

            base.Show();
        }
    }
}
