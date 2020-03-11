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
        public Image panelImageLeft;
        public Image panelImageRight;
        public Image backgroundImage;
        public Image[] elementalTypeImages;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI optionText;
        public SimpleCountableItemView itemView;
        public GameObject lockParent;

        public EquipmentItemRecipeSheet.Row model;
        public ItemSubType itemSubType;
        public ElementalType elementalType;

        public readonly Subject<EquipmentRecipeCellView> OnClick = new Subject<EquipmentRecipeCellView>();

        private readonly Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

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

        public void ShowLocked()
        {
            SetLocked(true);
            SetPanelDimmed(true);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Set(EquipmentItemRecipeSheet.Row recipeRow, bool isAvailable)
        {
            if (recipeRow is null)
                return;

            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
                return;

            model = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            itemSubType = row.ItemSubType;
            elementalType = row.ElementalType;

            titleText.text = equipment.GetLocalizedNonColoredName();

            var item = new CountableItem(equipment, 1);
            itemView.SetData(item);
            SetLocked(false);
            SetDimmed(!isAvailable);

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

        public void SetLocked(bool value)
        {
            lockParent.SetActive(value);
            itemView.gameObject.SetActive(!value);
            titleText.enabled = !value;
            optionText.enabled = !value;

            foreach (var icon in elementalTypeImages)
            {
                icon.enabled = !value;
            }
        }

        public void SetDimmed(bool value)
        {
            var color = value ? disabledColor : Color.white;
            titleText.color = itemView.Model.ItemBase.Value.GetItemGradeColor() * color;
            optionText.color = color;
            itemView.Model.Dimmed.Value = value;

            foreach (var icon in elementalTypeImages)
            {
                icon.color = value ? disabledColor : Color.white;
            }

            SetPanelDimmed(value);
        }

        private void SetPanelDimmed(bool value)
        {
            var color = value ? disabledColor : Color.white;
            panelImageLeft.color = color;
            panelImageRight.color = color;
            backgroundImage.color = color;
        }
    }
}
