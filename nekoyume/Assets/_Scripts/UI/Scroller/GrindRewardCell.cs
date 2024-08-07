using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class GrindRewardCell : GridCell<GrindRewardCell.Model, GrindRewardScroll.ContextModel>
    {
        public class Model
        {
            public (ItemBase itemBase, int count)? Item;
            public FungibleAssetValue? FungibleAssetValue;

            public Model((ItemBase itemBase, int count) item)
            {
                Item = item;
            }

            public Model(FungibleAssetValue value)
            {
                FungibleAssetValue = value;
            }
        }

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI countText;

        public override void UpdateContent(Model itemData)
        {
            if (itemData.Item is not null)
            {
                iconImage.overrideSprite = itemData.Item.Value.itemBase.GetIconSprite();
                nameText.text = itemData.Item.Value.itemBase.GetLocalizedName();
                countText.text = itemData.Item.Value.count.ToString();
            }
            else if (itemData.FungibleAssetValue is not null)
            {
                iconImage.overrideSprite = itemData.FungibleAssetValue.Value.GetIconSprite();
                nameText.text = itemData.FungibleAssetValue.Value.GetLocalizedName();
                countText.text = itemData.FungibleAssetValue.Value.MajorUnit.ToCurrencyNotation();
            }
        }
    }
}
