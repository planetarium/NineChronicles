using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ShopItemView : CountableItemView<ShopItem>
    {
        public GameObject priceGroup;
        public TextMeshProUGUI priceText;
        public override void SetData(ShopItem model)
        {
            if (model is null)
            {
                Clear();
                return;
            }
            
            base.SetData(model);

            SetBg(1f);
            priceGroup.SetActive(true);
            priceText.text = model.Price.Value.ToString("N0");

            Model.View = this;
        }

        public override void Clear()
        {
            base.Clear();
            SetBg(0f);
            priceGroup.SetActive(false);
        }

        private void SetBg(float alpha)
        {
            var a = alpha;
            var color = backgroundImage.color;
            color.a = a;
            backgroundImage.color = color;
        }
    }
}
