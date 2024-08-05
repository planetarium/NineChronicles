using Libplanet.Types.Assets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class GrindRewardCell : GridCell<GrindRewardCell.Model, GrindRewardScroll.ContextModel>
    {
        public class Model
        {
            public FungibleAssetValue FungibleAssetValue;
            public int Count;
        }

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI countText;

        public override void UpdateContent(Model itemData)
        {
            iconImage.overrideSprite = itemData.FungibleAssetValue.GetIconSprite();
            nameText.text = itemData.FungibleAssetValue.GetLocalizedName();
            countText.text = itemData.Count.ToCurrencyNotation();
        }
    }
}
