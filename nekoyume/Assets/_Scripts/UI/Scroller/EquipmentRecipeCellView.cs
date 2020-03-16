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

        [SerializeField]
        private Button button;

        [SerializeField]
        private Image panelImageLeft;

        [SerializeField]
        private Image panelImageRight;

        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image[] elementalTypeImages;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI optionText;

        [SerializeField]
        private SimpleCountableItemView itemView;

        [SerializeField]
        private GameObject lockParent;

        [SerializeField]
        private TextMeshProUGUI unlockConditionText;

        public readonly Subject<EquipmentRecipeCellView> OnClick = new Subject<EquipmentRecipeCellView>();

        private bool IsLocked => lockParent.activeSelf;
        public EquipmentItemRecipeSheet.Row RowData { get; private set; }
        public ItemSubType ItemSubType { get; private set; }
        public ElementalType ElementalType { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (IsLocked)
                    {
                        return;
                    }

                    OnClick.OnNext(this);
                })
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

            RowData = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            ItemSubType = row.ItemSubType;
            ElementalType = row.ElementalType;

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
            if (RowData is null)
                return;

            // 해금 검사.
            if (avatarState.worldInformation.TryGetLastClearedStageId(out var stageId))
            {
                if (RowData.UnlockStage > stageId)
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

            // 메인 재료 검사.
            var inventory = avatarState.inventory;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            if (materialSheet.TryGetValue(RowData.MaterialId, out var materialRow) &&
                inventory.TryGetFungibleItem(materialRow.ItemId, out var fungibleItem) &&
                fungibleItem.count >= RowData.MaterialCount)
            {
                // 서브 재료 검사.
                if (RowData.SubRecipeIds.Any())
                {
                    var subSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                    var shouldDimmed = false;
                    foreach (var subRow in RowData.SubRecipeIds
                        .Select(subRecipeId => subSheet.TryGetValue(subRecipeId, out var subRow) ? subRow : null)
                        .Where(item => !(item is null)))
                    {
                        foreach (var info in subRow.Materials)
                        {
                            if (materialSheet.TryGetValue(info.Id, out materialRow) &&
                                inventory.TryGetFungibleItem(materialRow.ItemId, out fungibleItem) &&
                                fungibleItem.count >= info.Count)
                            {
                                continue;
                            }

                            shouldDimmed = true;
                            break;
                        }

                        if (shouldDimmed)
                        {
                            break;
                        }
                    }

                    SetDimmed(shouldDimmed);
                }
                else
                {
                    SetDimmed(false);
                }
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
                ? string.Format(LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                    RowData.UnlockStage > 50
                        ? "???"
                        : RowData.UnlockStage.ToString())
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
