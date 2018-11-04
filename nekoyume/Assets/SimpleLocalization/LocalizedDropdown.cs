using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Localize dropdown component.
	/// </summary>
    [RequireComponent(typeof(Dropdown))]
    public class LocalizedDropdown : MonoBehaviour
    {
        public string[] LocalizationKeys;

        public void Start()
        {
            Localize();
            LocalizationManager.LocalizationChanged += Localize;
        }

        public void OnDestroy()
        {
            LocalizationManager.LocalizationChanged -= Localize;
        }

        private void Localize()
        {
	        var dropdown = GetComponent<Dropdown>();

			for (var i = 0; i < LocalizationKeys.Length; i++)
	        {
		        dropdown.options[i].text = LocalizationManager.Localize(LocalizationKeys[i]);
	        }

	        if (dropdown.value < LocalizationKeys.Length)
	        {
		        dropdown.captionText.text = LocalizationManager.Localize(LocalizationKeys[dropdown.value]);
	        }
        }
    }
}