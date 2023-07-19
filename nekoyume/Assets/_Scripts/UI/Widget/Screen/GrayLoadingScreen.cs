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
