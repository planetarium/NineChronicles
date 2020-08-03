using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class VanilaTooltip : Widget
    {
        public Image panelImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        protected override WidgetType WidgetType => WidgetType.Tooltip;

        public void Show(string title, string content, Vector2 position, bool localize = true)
        {
            titleText.text = L10nManager.Localize(title);
            contentText.text = L10nManager.Localize(content);
            panelImage.rectTransform.position = position;

            base.Show();
        }

        public void SetSize(float width, float height)
        {
            panelImage.rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}
