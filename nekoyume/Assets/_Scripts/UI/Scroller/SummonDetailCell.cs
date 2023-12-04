using Nekoyume.Helper;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class SummonDetailCell : RectCell<SummonDetailCell.Model, SummonDetailScroll.ContextModel>
    {
        public class Model
        {
            public EquipmentItemSheet.Row EquipmentRow;
            public float Ratio;
        }

        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI percentText;

        public override void UpdateContent(Model itemData)
        {
            iconImage.overrideSprite = SpriteHelper.GetItemIcon(itemData.EquipmentRow.Id);
            nameText.text = itemData.EquipmentRow.GetLocalizedName(true, false);
            percentText.text = itemData.Ratio.ToString("0.####%");
        }
    }
}
