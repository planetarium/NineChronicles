using System;
using System.Linq;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using TMPro;
using Nekoyume.TableData;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentRecipeCellView : MonoBehaviour
    {
        public Button button;
        public Image[] elementalTypeImages;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI optionText;
        public SimpleCountableItemView itemView;

        public EquipmentItemRecipeSheet.Row model;
        public ItemSubType itemSubType;
        public ElementalType elementalType;

        public readonly Subject<EquipmentRecipeCellView> OnClick = new Subject<EquipmentRecipeCellView>();

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                OnClick.OnNext(this);
            });
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Set(EquipmentItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
                return;

            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            var row = equipmentSheet.Values.FirstOrDefault(i => i.Id == recipeRow.ResultEquipmentId);
            if (row is null)
                return;

            model = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            itemSubType = row.ItemSubType;
            elementalType = row.ElementalType;

            titleText.text = equipment.GetLocalizedName();

            var item = new CountableItem(equipment, 1);
            itemView.SetData(item);

            var sprite = row.ElementalType.GetSprite();
            var grade = row.Grade;

            for (var i = 0; i < elementalTypeImages.Length; ++i)
            {
                if (sprite is null || i >= grade)
                {
                    elementalTypeImages[i].gameObject.SetActive(false);
                    continue;
                }

                elementalTypeImages[i].sprite = sprite;
                elementalTypeImages[i].gameObject.SetActive(true);
            }

            var text = $"{row.Stat.Type} +{row.Stat.Value}";
            optionText.text = text;
        }
    }
}
