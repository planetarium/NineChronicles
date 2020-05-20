using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private string localizationKey = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        private void Awake()
        {
            text.text = LocalizationManager.Localize(localizationKey);
        }

        private void Reset()
        {
            text = GetComponent<TextMeshProUGUI>();
        }
    }
}
