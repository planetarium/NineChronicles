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
    using System.Collections.Generic;
    using UniRx;

    public class RecipeCell : MonoBehaviour
    {
        [SerializeField] private Animator animator = null;
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private GameObject selectedObject = null;
        [SerializeField] private GameObject lockObject = null;
        [SerializeField] private GameObject lockVFXObject = null;
        [SerializeField] private GameObject lockOpenVFXObject = null;
        [SerializeField] private GameObject indicatorObject = null;
        [SerializeField] private TextMeshProUGUI unlockConditionText = null;
        [SerializeField] private Button button = null;
        [SerializeField] private bool selectable = true;

        private SheetRow<int> _recipeRow = null;
        private bool _unlockable = false;
        private int _recipeIdToUnlock;

        private readonly List<IDisposable> _disposablesForOnDisable = new List<IDisposable>();

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
                    if (!IsLocked)
                    {
                        AudioController.PlayClick();
                        Craft.SharedModel.SelectedRow.Value = _recipeRow;
                    }
                    else if (_unlockable)
                    {
                        Unlock();
                    }
                    else
                    {
                        if (_recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
                        {
                            var format = L10nManager.Localize("UI_REQUIRE_CLEAR_STAGE");
                            var message = string.Format(format, equipmentRow.UnlockStage);
                            OneLinePopup.Push(MailType.System, message);
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
            _unlockable = false;
            _recipeRow = recipeRow;

            var tableSheets = Game.Game.instance.TableSheets;

            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultEquipmentItemRow();
                var viewData = recipeViewData.GetData(resultItem.Grade);
                equipmentView.Show(viewData, resultItem);
                consumableView.Hide();
                if (checkLocked)
                {
                    UpdateLocked(equipmentRow);
                }
                else
                {
                    IsLocked = false;
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

            gameObject.SetActive(true);

            if (selectable)
            {
                var selected = Craft.SharedModel.SelectedRow;
                var notified = Craft.SharedModel.NotifiedRow;

                if(!IsLocked) SetSelected(selected.Value);
                selected.Subscribe(SetSelected)
                    .AddTo(_disposablesForOnDisable);

                SetNotified(notified.Value);
                notified.Subscribe(SetNotified)
                    .AddTo(_disposablesForOnDisable);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateLocked(EquipmentItemRecipeSheet.Row equipmentRow)
        {
            lockVFXObject.SetActive(false);
            unlockConditionText.enabled = false;
            var worldInformation = States.Instance.CurrentAvatarState.worldInformation;

            var unlockStage = equipmentRow.UnlockStage;
            var clearedStage = worldInformation.TryGetLastClearedStageId(out var stageId) ?
                stageId : 0;

            var diff = unlockStage - clearedStage;

            if (diff > 0)
            {
                unlockConditionText.text = string.Format(
                    L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                    diff > 50 ? "???" : unlockStage.ToString());
                unlockConditionText.enabled = true;
                equipmentView.Hide();
                IsLocked = true;
                return;
            }
            else if (!Craft.SharedModel.RecipeVFXSkipList.Contains(equipmentRow.Id))
            {
                _recipeIdToUnlock = equipmentRow.Id;
                lockVFXObject.SetActive(true);
                equipmentView.Hide();
                IsLocked = true;
                _unlockable = true;
                return;
            }

            IsLocked = false;
        }

        public void Unlock()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
            lockOpenVFXObject.SetActive(true);
            Craft.SharedModel.RecipeVFXSkipList.Add(_recipeIdToUnlock);
            Craft.SharedModel.SaveRecipeVFXSkipList();

            equipmentView.gameObject.SetActive(true);
            IsLocked = false;
            _unlockable = false;
        }

        public void SetSelected(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            selectedObject.SetActive(equals);
            if (equals)
            {
                Craft.SharedModel.SelectedRecipeCell = this;
                animator.SetTrigger("Clicked");
            }
            else
            {
                animator.SetTrigger("Normal");
            }
        }

        public void SetNotified(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            indicatorObject.SetActive(equals);
        }
    }
}
