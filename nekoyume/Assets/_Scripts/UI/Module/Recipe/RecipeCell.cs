using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.TableData;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.State;
using TMPro;
using System;
using Nekoyume.Model.Mail;

namespace Nekoyume.UI.Module
{
    using Nekoyume.L10n;
    using Nekoyume.UI.Scroller;
    using System.Collections.Generic;
    using UniRx;

    public class RecipeCell : MonoBehaviour
    {
        [SerializeField] private Animator animator = null;
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private RecipeView loadingView = null;
        [SerializeField] private GameObject selectedObject = null;
        [SerializeField] private GameObject unlockObject = null;
        [SerializeField] private GameObject lockObject = null;
        [SerializeField] private GameObject lockVFXObject = null;
        [SerializeField] private GameObject lockOpenVFXObject = null;
        [SerializeField] private GameObject notificationObject = null;
        [SerializeField] private TextMeshProUGUI unlockConditionText = null;
        [SerializeField] private TextMeshProUGUI unlockPriceText = null;
        [SerializeField] private Button button = null;
        [SerializeField] private bool selectable = true;

        private SheetRow<int> _recipeRow = null;
        private bool _unlockable = false;
        private bool _isWaitingForUnlock = false;

        private readonly List<IDisposable> _disposablesForOnDisable = new List<IDisposable>();

        public int RecipeId => _recipeRow.Key;

        private bool IsLocked {
            get => lockObject.activeSelf;
            set
            {
                lockObject.SetActive(value);
            }
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
            _disposablesForOnDisable.DisposeAllAndClear();
        }

        public void Show(SheetRow<int> recipeRow, bool checkLocked = true)
        {
            _recipeRow = recipeRow;
            var tableSheets = Game.Game.instance.TableSheets;

            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                IsLocked = true;
                if (checkLocked)
                {
                    UpdateLocked(equipmentRow);
                }
                else
                {
                    IsLocked = false;
                    SetEquipmentView(equipmentRow);
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

                if (!IsLocked) SetSelected(selected.Value);
                selected.Subscribe(SetSelected)
                    .AddTo(_disposablesForOnDisable);

                SetNotified(notified.Value);
                notified.Subscribe(SetNotified)
                    .AddTo(_disposablesForOnDisable);

                unlocked.Subscribe(SetUnlocked)
                    .AddTo(_disposablesForOnDisable);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateLocked(EquipmentItemRecipeSheet.Row equipmentRow)
        {
            animator.Rebind();
            loadingView.gameObject.SetActive(false);
            lockVFXObject.SetActive(false);
            unlockConditionText.enabled = false;
            unlockObject.SetActive(false);
            _unlockable = false;
            _isWaitingForUnlock = false;
            var avatarState = States.Instance.CurrentAvatarState;
            var worldInformation = avatarState.worldInformation;
            
            var unlockStage = equipmentRow.UnlockStage;
            var clearedStage = worldInformation.TryGetLastClearedStageId(out var stageId) ?
                stageId : 0;
            var diff = unlockStage - clearedStage;

            if (diff > 0)
            {
                unlockConditionText.text = unlockStage != 999
                    ? string.Format(
                        L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        unlockStage.ToString())
                    : string.Empty;
                unlockConditionText.enabled = true;
                equipmentView.Hide();
                IsLocked = true;
                return;
            }
            else if (Craft.SharedModel.DummyLockedRecipes.Contains(equipmentRow.Id))
            {
                lockVFXObject.SetActive(true);
                equipmentView.Hide();
                IsLocked = true;
                _unlockable = true;
                return;
            }
            else if (Craft.SharedModel.UnlockedRecipes is null)
            {
                unlockConditionText.text = L10nManager.Localize("ERROR_FAILED_LOAD_STATE");
                unlockConditionText.enabled = true;
                equipmentView.Hide();
                IsLocked = true;
                return;
            }
            else if (Craft.SharedModel.UnlockingRecipes.Contains(equipmentRow.Id))
            {
                _isWaitingForUnlock = true;
                SetLoadingView(equipmentRow);
                IsLocked = false;
                return;
            }
            else if (!Craft.SharedModel.UnlockedRecipes.Value.Contains(equipmentRow.Id))
            {
                lockVFXObject.SetActive(true);
                equipmentView.Hide();
                unlockObject.SetActive(true);
                unlockPriceText.text = equipmentRow.CRYSTAL.ToString();
                IsLocked = true;
                _unlockable = true;
                return;
            }

            SetEquipmentView(equipmentRow);
            IsLocked = false;
        }

        private void SetEquipmentView(EquipmentItemRecipeSheet.Row row)
        {
            var resultItem = row.GetResultEquipmentItemRow();
            var viewData = recipeViewData.GetData(resultItem.Grade);
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

            lockObject.SetActive(false);
            SetLoadingView(_recipeRow as EquipmentItemRecipeSheet.Row);
        }

        public void UnlockDummyLocked()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
            lockOpenVFXObject.SetActive(true);
            lockObject.SetActive(false);
            Craft.SharedModel.DummyLockedRecipes.Remove(RecipeId);
            SetEquipmentView(_recipeRow as EquipmentItemRecipeSheet.Row);
        }

        public void SetSelected(SheetRow<int> row)
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

        public void SetNotified(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            notificationObject.SetActive(equals);
        }

        public void SetUnlocked(List<int> recipeIds)
        {
            if (_recipeRow is EquipmentItemRecipeSheet.Row row)
            {
                UpdateLocked(row);
            }
        }
    }
}
