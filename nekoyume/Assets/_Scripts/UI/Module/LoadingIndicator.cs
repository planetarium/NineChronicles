using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class LoadingIndicator : MonoBehaviour
    {
        public TextMeshProUGUI text;

        public void Show(string key)
        {
            text.text = LocalizationManager.Localize(key);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
