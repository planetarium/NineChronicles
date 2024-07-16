using Nekoyume.Helper;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RuneStone : AlphaAnimateModule
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI count;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private Button button;

        private string _ticker;

        private void Awake()
        {
            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        public void SetActiveLoading(bool value)
        {
            loadingObject.SetActive(value);
            count.gameObject.SetActive(!value);
        }

        public void SetRuneStone(Sprite icon, string quantity, string ticker)
        {
            iconImage.sprite = icon;
            count.text = quantity;
            _ticker = ticker;
        }

        private void ShowMaterialNavigationPopup()
        {
            if (!RuneFrontHelper.TryGetRuneData(_ticker, out var runeData))
            {
                return;
            }

            Widget.Find<MaterialNavigationPopup>().ShowRuneStone(runeData.id);
        }
    }
}
