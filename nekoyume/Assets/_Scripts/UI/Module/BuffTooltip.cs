using UnityEngine;
using TMPro;
using Nekoyume.L10n;
using Nekoyume.Model.Buff;

namespace Nekoyume.UI.Module
{
    public class BuffTooltip : MonoBehaviour
    {
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI durationText;
        public RectTransform RectTransform { get; private set; }

        private const string descriptionFormat = "{0}\n{1}";
        private string durationFormat;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            durationFormat = L10nManager.Localize("UI_REMAININGTURN");
        }

        public void UpdateText(Buff data)
        {
            var name = data.GetLocalizedName();
            var description = data.GetLocalizedDescription();

            descriptionText.text = string.Format(descriptionFormat, name, description);
            durationText.text = string.Format(durationFormat, data.RemainedDuration);
        }
    }
}
