using Nekoyume.Helper;
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

        [SerializeField]
        private Button button;

        private CostType _costType;

        private void Awake()
        {
            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        public void SetMaterial(Sprite icon, int quantity, CostType costType)
        {
            iconImage.sprite = icon;
            countText.text = TextHelper.FormatNumber(quantity);
            _costType = costType;
        }

        private void ShowMaterialNavigationPopup()
        {
            Widget.Find<MaterialNavigationPopup>().ShowCurrency(_costType);
        }
    }
}
