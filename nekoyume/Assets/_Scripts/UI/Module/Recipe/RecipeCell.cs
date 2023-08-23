using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.TableData;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using TMPro;
using System;
using Nekoyume.Model.Mail;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using Nekoyume.TableData.Event;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class RecipeCell : MonoBehaviour
    {
        [Serializable]
        public class LockedObject
        {
            public GameObject container;
            public Image gradeBg;
            public GameObject vfxObject;
            public TextMeshProUGUI unlockConditionText;
            public GameObject unlockInfoContainer;
            public TextMeshProUGUI unlockPriceText;
        }

        [SerializeField] private Button button = null;
        [SerializeField] private Animator animator = null;
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private GameObject gradeEffectObject = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private LockedObject lockedObject = null;
        [SerializeField] private GameObject notificationObject = null;
        [SerializeField] private RecipeView loadingView = null;
        [SerializeField] private GameObject lockOpenVFXObject = null;
        [SerializeField] private GameObject selectedObject = null;
        [SerializeField] private bool selectable = true;

        private SheetRow<int> _recipeRow = null;
        private bool _unlockable = false;
        private bool _isWaitingForUnlock = false;

        private readonly List<IDisposable> _disposablesForOnDisable = new List<IDisposable>();

        public int RecipeId => _recipeRow.Key;

        private bool IsLocked
        {
            get => lockedObject.container.activeSelf;
            set => lockedObject.container.SetActive(value);
        }

        private void Awake()
        {
            if (selectable)
            {
                button.onClick.AddListener(() =>
                {
                    if (!IsLocked || _unlockable)
                    {
                        AudioController.PlayClick();
                        Craft.SharedModel.SelectedRow.Value = _recipeRow;
                    }
                    else
                    {
                        if (_recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
                        {
                            if (equipmentRow.UnlockStage == 999)
                            {
                                OneLineSystem.Push(
                                    MailType.System,
                                    L10nManager.Localize("UI_RECIPE_LOCK_GUIDE"),
                                    NotificationCell.NotificationType.UnlockCondition);
                            }
                            else
                            {
                                OneLineSystem.Push(
                                    MailType.System,
                                    L10nManager.Localize("UI_REQUIRE_CLEAR_STAGE",
                                        equipmentRow.UnlockStage),
                                    NotificationCell.NotificationType.UnlockCondition);
                            }
                        }
                    }
                });
            }
        }

        private void OnDisable()
        {
            selectedObject.SetActive(false);
            gradeEffectObject.SetActive(false);
            _disposablesForOnDisable.DisposeAllAndClear();
        }

        public void Show(SheetRow<int> recipeRow, bool checkLocked = true)
        {
            _recipeRow = recipeRow;
            var tableSheets = Game.Game.instance.TableSheets;

            loadingView.Hide();
            gradeEffectObject.SetActive(false);
            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                consumableView.Hide();
                IsLocked = true;

                var resultItem = equipmentRow.GetResultEquipmentItemRow();
                if (checkLocked)
                {
                    UpdateLocked(equipmentRow, resultItem);
                }
                else
                {
                    IsLocked = false;
                    SetEquipmentView(resultItem);
                }
            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultConsumableItemRow();
                var viewData = recipeViewData.GetData(resultItem.Grade);
                equipmentView.Hide();
                consumableView.Show(viewData, resultItem);
                IsLocked = false;
            }
            else if (recipeRow is EventMaterialItemRecipeSheet.Row materialRow)
            {
                var resultItem = materialRow.GetResultMaterialItemRow();
                var viewData = recipeViewData.GetData(resultItem.Grade);
                equipmentView.Hide();
                consumableView.Show(viewData, resultItem, materialRow.ResultMaterialItemCount);
                IsLocked = false;
            }
            else
            {
                Debug.LogError($"Not supported type of recipe.");
                IsLocked = true;
            }

            lockOpenVFXObject.SetActive(false);
            gameObject.SetActive(true);

            if (selectable)
            {
                var selected = Craft.SharedModel.SelectedRow;
                var notified = Craft.SharedModel.NotifiedRow;
                var unlocked = Craft.SharedModel.UnlockedRecipes;
                var unlockable = Craft.SharedModel.UnlockableRecipes;

                if (!IsLocked) SetSelected(selected.Value);
                selected.Subscribe(SetSelected)
                    .AddTo(_disposablesForOnDisable);

                SetNotified(notified.Value);
                notified.Subscribe(SetNotified)
                    .AddTo(_disposablesForOnDisable);

                unlocked.Subscribe(SetUnlocked)
                    .AddTo(_disposablesForOnDisable);

                unlockable.Subscribe(SetUnlockable)
                    .AddTo(_disposablesForOnDisable);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateLocked(EquipmentItemRecipeSheet.Row equipmentRow, ItemSheet.Row resultItem)
        {
            animator.Rebind();
            lockedObject.gradeBg.sprite = recipeViewData.GetData(resultItem.Grade).BgSprite;
            lockedObject.vfxObject.SetActive(false);
            lockedObject.unlockConditionText.enabled = false;
            lockedObject.unlockInfoContainer.SetActive(false);
            _unlockable = false;
            _isWaitingForUnlock = false;
            var avatarState = States.Instance.CurrentAvatarState;
            var worldInformation = avatarState.worldInformation;

            var unlockStage = equipmentRow.UnlockStage;
            var clearedStage = worldInformation.TryGetLastClearedStageId(out var stageId) ? stageId : 0;
            var diff = unlockStage - clearedStage;
            var sharedModel = Craft.SharedModel;

            if (equipmentRow.CRYSTAL == 0)
            {
                SetEquipmentView(resultItem);
                IsLocked = false;
                return;
            }

            if (diff > 0)
            {
                lockedObject.unlockConditionText.text = unlockStage != 999
                    ? string.Format(
                        L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        unlockStage.ToString())
                    : string.Empty;
                lockedObject.unlockConditionText.enabled = true;
                equipmentView.Hide();
                IsLocked = true;
                return;
            }

            if (sharedModel.DummyLockedRecipes.Contains(equipmentRow.Id))
            {
                lockedObject.vfxObject.SetActive(true);
                equipmentView.Hide();
                IsLocked = true;
                _unlockable = true;
                return;
            }

            if (sharedModel.UnlockedRecipes is null)
            {
                lockedObject.unlockConditionText.text = L10nManager.Localize("ERROR_FAILED_LOAD_STATE");
                lockedObject.unlockConditionText.enabled = true;
                equipmentView.Hide();
                IsLocked = true;
                return;
            }

            if (sharedModel.UnlockingRecipes.Contains(equipmentRow.Id))
            {
                _isWaitingForUnlock = true;
                SetLoadingView(equipmentRow);
                IsLocked = false;
                return;
            }

            if (!sharedModel.UnlockedRecipes.Value.Contains(equipmentRow.Id))
            {
                var unlockable =
                    sharedModel.UnlockableRecipes.Value is not null &&
                    sharedModel.UnlockableRecipes.Value.Contains(equipmentRow.Id) &&
                    sharedModel.UnlockableRecipesOpenCost <=
                    States.Instance.CrystalBalance.MajorUnit;
                lockedObject.vfxObject.SetActive(unlockable);
                equipmentView.Hide();
                lockedObject.unlockInfoContainer.SetActive(true);
                lockedObject.unlockPriceText.text = equipmentRow.CRYSTAL.ToString();
                lockedObject.unlockPriceText.color = unlockable
                    ? Palette.GetColor(ColorType.ButtonEnabled)
                    : Palette.GetColor(ColorType.ButtonDisabled);
                IsLocked = true;
                _unlockable = true;
                return;
            }

            SetEquipmentView(resultItem);
            IsLocked = false;
        }

        private void SetEquipmentView(ItemSheet.Row resultItem)
        {
            var viewData = recipeViewData.GetData(resultItem.Grade);
            gradeEffectObject.SetActive(resultItem.Grade >= 5);
            equipmentView.Show(viewData, resultItem);
            consumableView.Hide();
            loadingView.Hide();
        }

        private void SetLoadingView(EquipmentItemRecipeSheet.Row row)
        {
            var resultItem = row.GetResultEquipmentItemRow();
            var viewData = recipeViewData.GetData(resultItem.Grade);
            equipmentView.Hide();
            consumableView.Hide();
            loadingView.Show(viewData, resultItem);
        }

        public void Unlock()
        {
            _isWaitingForUnlock = true;
            AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
            lockOpenVFXObject.SetActive(true);

            lockedObject.container.SetActive(false);
            SetLoadingView(_recipeRow as EquipmentItemRecipeSheet.Row);
        }

        public void UnlockDummyLocked()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
            lockOpenVFXObject.SetActive(true);
            lockedObject.container.SetActive(false);
            Craft.SharedModel.DummyLockedRecipes.Remove(RecipeId);
            var row = _recipeRow as EquipmentItemRecipeSheet.Row;
            var resultItem = row.GetResultEquipmentItemRow();
            SetEquipmentView(resultItem);
        }

        private void SetSelected(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            selectedObject.SetActive(equals);
            if (!_isWaitingForUnlock)
            {
                if (equals)
                {
                    animator.Rebind();
                    animator.SetTrigger("Clicked");
                    Craft.SharedModel.SelectedRecipeCell = this;
                }
                else
                {
                    animator.Rebind();
                    animator.SetTrigger("Normal");
                }
            }
        }

        private void SetNotified(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            notificationObject.SetActive(equals);
        }

        private void SetUnlocked(List<int> recipeIds)
        {
            if (_recipeRow is EquipmentItemRecipeSheet.Row row)
            {
                var resultItem = row.GetResultEquipmentItemRow();
                UpdateLocked(row, resultItem);
            }
        }

        private void SetUnlockable(List<int> recipeIds)
        {
            var unlockable =
                recipeIds is not null &&
                recipeIds.Contains(_recipeRow.Key) &&
                Craft.SharedModel.UnlockableRecipesOpenCost <=
                States.Instance.CrystalBalance.MajorUnit;
            lockedObject.vfxObject.SetActive(unlockable);
            lockedObject.unlockPriceText.color = unlockable
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);
        }
    }
}
