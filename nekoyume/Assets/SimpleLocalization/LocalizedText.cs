using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleLocalization
{
    /// <summary>
    /// Localize text component.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string LocalizationKey;

        public void Start()
        {
            Localize();
            LocalizationManager.OnChangeLanguage += Localize;
        }

        public void OnDestroy()
        {
            LocalizationManager.OnChangeLanguage -= Localize;
        }

        private void Localize()
        {
            GetComponent<Text>().text = LocalizationManager.Localize(LocalizationKey);
        }
    }
}
