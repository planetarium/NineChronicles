using Nekoyume.Helper;
using Nekoyume.L10n;
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
            public string RuneTicker;
            public float Ratio;
        }

        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI percentText;

        public override void UpdateContent(Model itemData)
        {
            if (itemData.EquipmentRow is not null)
            {
                iconImage.overrideSprite = SpriteHelper.GetItemIcon(itemData.EquipmentRow.Id);
                nameText.text = itemData.EquipmentRow.GetLocalizedName(true, false);
            }

            if (!string.IsNullOrEmpty(itemData.RuneTicker) &&
                RuneFrontHelper.TryGetRuneData(itemData.RuneTicker, out var runeData) &&
                Game.Game.instance.TableSheets.RuneListSheet.TryGetValue(runeData.id, out var row))
            {
                nameText.text = $"<color=#{LocalizationExtensions.GetColorHexByGrade(row.Grade)}>" +
                    $"{L10nManager.Localize($"RUNE_NAME_{row.Id}")}</color>";
                iconImage.overrideSprite = runeData.icon;
            }

            percentText.text = itemData.Ratio.ToString("0.####%");
        }
    }
}
