using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    // TODO: L10nManager 쪽으로 이동시켜서 초기화나 언어 변경을 구독하게 합니다.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private string localizationKey = null;

        [SerializeField]
        private TextMeshProUGUI text = null;

        private void Awake()
        {
            text.text =
                L10nManager.Localize(localizationKey);
        }

        private void Reset()
        {
            text = GetComponent<TextMeshProUGUI>();
        }
    }
}
