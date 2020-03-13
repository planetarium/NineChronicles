using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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
        private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f);

        public Button button;
        public Image panelImageLeft;
        public Image panelImageRight;
        public Image backgroundImage;
        public Image[] elementalTypeImages;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI optionText;
        public SimpleCountableItemView itemView;
        public GameObject lockParent;
        public TextMeshProUGUI unlockConditionText;

        public EquipmentItemRecipeSheet.Row model;
        public ItemSubType itemSubType;
        public ElementalType elementalType;

        public readonly Subject<EquipmentRecipeCellView> OnClick = new Subject<EquipmentRecipeCellView>();

        private void Awake()
        {
            button.OnClickAsObservable()
                .Subscribe(_ => OnClick.OnNext(this))
                .AddTo(gameObject);
        }

        private void OnDestroy()
        {
            OnClick.Dispose();
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
            if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
                return;

            model = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            itemSubType = row.ItemSubType;
            elementalType = row.ElementalType;

            titleText.text = equipment.GetLocalizedNonColoredName();

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
            
            SetLocked(false);
            SetDimmed(false);
        }

        public void Set(AvatarState avatarState)
        {
            if (model is null)
                return;

            // 해금 검사.
            if (avatarState.worldInformation.TryGetLastClearedStageId(out var stageId))
            {
                if (model.UnlockStage > stageId)
                {
                    SetLocked(true);
                    return;
                }

                SetLocked(false);
            }
            else
            {
                SetLocked(true);
                return;
            }

            // 재료 검사.
            if (Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(model.MaterialId, out var materialRow) &&
                avatarState.inventory.TryGetFungibleItem(materialRow.ItemId, out var material) &&
                material.count >= model.MaterialCount)
            {
                SetDimmed(false);
            }
            else
            {
                SetDimmed(true);
            }
        }

        private void SetLocked(bool value)
        {
            lockParent.SetActive(value);
            unlockConditionText.text = value
                ? string.Format(LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"), model.UnlockStage)
                : string.Empty;
            
            itemView.gameObject.SetActive(!value);
            titleText.enabled = !value;
            optionText.enabled = !value;

            foreach (var icon in elementalTypeImages)
            {
                icon.enabled = !value;
            }

            SetPanelDimmed(value);
        }

        private void SetDimmed(bool value)
        {
            var color = value ? DisabledColor : Color.white;
            titleText.color = itemView.Model.ItemBase.Value.GetItemGradeColor() * color;
            optionText.color = color;
            itemView.Model.Dimmed.Value = value;

            foreach (var icon in elementalTypeImages)
            {
                icon.color = value ? DisabledColor : Color.white;
            }

            SetPanelDimmed(value);
        }

        private void SetPanelDimmed(bool value)
        {
            var color = value ? DisabledColor : Color.white;
            panelImageLeft.color = color;
            panelImageRight.color = color;
            backgroundImage.color = color;
        }
    }
}
