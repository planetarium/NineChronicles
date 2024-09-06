using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RandomOutfitCell : RectCell<RandomOutfitCell.Model, RectScrollDefaultContext>
    {
        public class Model
        {
            public int IconId;
            public string Ratio;

            public Model(int argIconId, string argRatio)
            {
                IconId = argIconId;
                Ratio = argRatio;
            }
        }

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI outfitNameText;

        [SerializeField]
        private TextMeshProUGUI ratioText;

        public override void UpdateContent(Model itemData)
        {
            iconImage.overrideSprite = SpriteHelper.GetItemIcon(itemData.IconId);
            outfitNameText.SetText(L10nManager.LocalizeItemName(itemData.IconId));
            ratioText.SetText(itemData.Ratio);
        }
    }
}
