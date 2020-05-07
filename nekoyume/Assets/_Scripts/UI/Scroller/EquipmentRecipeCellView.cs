using System;
using System.Linq;
using System.Text;
using Assets.SimpleLocalization;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
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

        [SerializeField]
        private CanvasGroup canvasGroup;

        public readonly Subject<EquipmentRecipeCellView> OnClick =
            new Subject<EquipmentRecipeCellView>();

        private bool IsLocked => lockParent.activeSelf;
        public EquipmentItemRecipeSheet.Row RowData { get; private set; }
        public ItemSubType ItemSubType { get; private set; }
        public ElementalType ElementalType { get; private set; }

        public bool Visible
        {
            get => Mathf.Approximately(canvasGroup.alpha, 1f);
            set => canvasGroup.alpha = value ? 1f : 0f;
        }

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
            Visible = true;
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
            Set(equipment);
        }

        public void Set(ConsumableItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
                return;

            var sheet = Game.Game.instance.TableSheets.ConsumableItemSheet;
            if (!sheet.TryGetValue(recipeRow.ResultConsumableItemId, out var row))
                return;

            var consumable = (Consumable) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(consumable);
        }

        private void Set(ItemUsable itemUsable)
        {
            ItemSubType = itemUsable.Data.ItemSubType;
            ElementalType = itemUsable.Data.ElementalType;

            titleText.text = itemUsable.GetLocalizedNonColoredName();

            var item = new CountableItem(itemUsable, 1);
            itemView.SetData(item);

            var sprite = ElementalType.GetSprite();
            var grade = itemUsable.Data.Grade;

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

            switch (itemUsable)
            {
                case Equipment equipment:
                {
                    var text = $"{equipment.Data.Stat.Type} +{equipment.Data.Stat.Value}";
                    optionText.text = text;
                    break;
                }
                case Consumable consumable:
                {
                    var sb = new StringBuilder();
                    foreach (var stat in consumable.Data.Stats)
                    {
                        sb.AppendLine($"{stat.StatType} +{stat.Value}");
                    }
                    optionText.text = sb.ToString();
                    break;
                }
            }

            SetLocked(false);
            SetDimmed(false);
        }

        public void Set(AvatarState avatarState)
        {
            if (RowData is null)
                return;

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(RowData.UnlockStage))
            {
                SetLocked(true);
                return;
            }

            SetLocked(false);

            // 메인 재료 검사.
            var inventory = avatarState.inventory;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            if (materialSheet.TryGetValue(RowData.MaterialId, out var materialRow) &&
                inventory.TryGetMaterial(materialRow.ItemId, out var fungibleItem) &&
                fungibleItem.count >= RowData.MaterialCount)
            {
                // 서브 재료 검사.
                if (RowData.SubRecipeIds.Any())
                {
                    var subSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                    var shouldDimmed = false;
                    foreach (var subRow in RowData.SubRecipeIds
                        .Select(subRecipeId =>
                            subSheet.TryGetValue(subRecipeId, out var subRow) ? subRow : null)
                        .Where(item => !(item is null)))
                    {
                        foreach (var info in subRow.Materials)
                        {
                            if (materialSheet.TryGetValue(info.Id, out materialRow) &&
                                inventory.TryGetMaterial(materialRow.ItemId,
                                    out fungibleItem) &&
                                fungibleItem.count >= info.Count)
                            {
                                continue;
                            }

                            shouldDimmed = true;
                            break;
                        }

                        if (!shouldDimmed)
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

        public void SetInteractable(bool value)
        {
            button.interactable = value;
        }

        private void SetLocked(bool value)
        {
            // TODO: 나중에 해금 시스템이 분리되면 아래의 해금 조건 텍스트를 얻는 로직을 옮겨서 반복을 없애야 좋겠다.
            if (value)
            {
                unlockConditionText.enabled = true;

                if (RowData is null)
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }

                if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var stageId))
                {
                    var diff = RowData.UnlockStage - stageId;
                    if (diff > 50)
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            "???");
                    }
                    else
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            RowData.UnlockStage.ToString());
                    }
                }
                else
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }
            }
            else
            {
                unlockConditionText.enabled = false;
            }

            lockParent.SetActive(value);
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
