using System.Globalization;
using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ArenaPendingNCG : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI arenaText;
        [SerializeField]
        private TextMeshProUGUI ncgText;

        private void Awake()
        {
            arenaText.text = LocalizationManager.Localize("UI_ARENA_FOUNDATION");
        }

        public void Show()
        {
            gameObject.SetActive(true);    
        }

        public void Show(decimal ncg)
        {
            ncgText.text = ncg.ToString(CultureInfo.InvariantCulture);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
