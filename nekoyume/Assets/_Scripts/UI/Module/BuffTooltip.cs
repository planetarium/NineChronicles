using UnityEngine;
using TMPro;
using Assets.SimpleLocalization;
using Nekoyume.TableData;
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
            durationFormat = LocalizationManager.Localize("UI_REMAININGTURN");
        }

        public void UpdateText(Buff data)
        {
            var name = data.RowData.GetLocalizedName();
            var description = data.RowData.GetLocalizedDescription();

            descriptionText.text = string.Format(descriptionFormat, name, description);
            durationText.text = string.Format(durationFormat, data.remainedDuration);
        }
    }
}
