using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class MaterialAsset : AlphaAnimateModule
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI countText;

        public void SetMaterial(Sprite icon, int quantity)
        {
            iconImage.sprite = icon;
            countText.text = quantity.ToString("N0");
        }
    }
}
