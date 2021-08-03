using System.Linq;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            SetLevel(model.ItemBase.Value.Grade, model.Level.Value);
            priceGroup.SetActive(true);
            priceText.text = model.Price.Value.GetQuantityString();
            Model.View = this;
        }

        public override void Clear()
        {
            if (Model != null)
            {
                Model.Selected.Value = false;
            }

            base.Clear();

            SetBg(0f);
            SetLevel(0, 0);
            priceGroup.SetActive(false);
        }

        private void SetBg(float alpha)
        {
            var a = alpha;
            var color = backgroundImage.color;
            color.a = a;
            backgroundImage.color = color;
        }

        private void SetLevel(int grade, int level)
        {
            if (level > 0)
            {
                var data = itemViewData.GetItemViewData(grade);
                enhancementImage.GetComponent<Image>().material = data.EnhancementMaterial;
                enhancementImage.SetActive(true);
                enhancementText.text = $"+{level}";
                enhancementText.enabled = true;
            }
        }
    }
}
