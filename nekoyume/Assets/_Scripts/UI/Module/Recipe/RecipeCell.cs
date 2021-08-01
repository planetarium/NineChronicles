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

namespace Nekoyume.UI.Module
{
    using Nekoyume.L10n;
    using UniRx;

    public class RecipeCell : MonoBehaviour
    {
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private GameObject selectedObject = null;
        [SerializeField] private GameObject lockObject = null;
        [SerializeField] private GameObject lockVFXObject = null;
        [SerializeField] private TextMeshProUGUI unlockConditionText = null;
        [SerializeField] private Button button = null;
        [SerializeField] private bool selectable = true;

        private RecipeView _hiddenView = null;
        private SheetRow<int> _recipeRow = null;
        private IDisposable _disposableForOnDisable = null;
        private bool _unlockable = false;
        private int _recipeIdToUnlock;

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
                        Unlock(_recipeIdToUnlock);
                    }
                });
            }
        }

        private void OnDisable()
        {
            selectedObject.SetActive(false);
            _disposableForOnDisable?.Dispose();
        }

        public void Show(SheetRow<int> recipeRow, bool checkLocked = true)
        {
            _unlockable = false;
            _recipeRow = recipeRow;

            var tableSheets = Game.Game.instance.TableSheets;

            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultItemEquipmentRow();
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
                var resultItem = consumableRow.GetResultItemConsumableRow();
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
                var property = Craft.SharedModel.SelectedRow;
                if(!IsLocked) SetSelected(property.Value);
                _disposableForOnDisable = property
                    .Subscribe(SetSelected);
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
            var worldInformation = ReactiveAvatarState.WorldInformation.Value;

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
                _hiddenView = equipmentView;
                IsLocked = true;
                return;
            }
            else if (!Craft.SharedModel.RecipeVFXSkipList.Contains(equipmentRow.Id))
            {
                _unlockable = true;
                _recipeIdToUnlock = equipmentRow.Id;
                lockVFXObject.SetActive(true);
                equipmentView.Hide();
                _hiddenView = equipmentView;
                IsLocked = true;
                return;
            }

            IsLocked = false;
        }

        public void Unlock(int recipeId)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
            var centerPos = GetComponent<RectTransform>()
                .GetWorldPositionOfCenter();
            VFXController.instance.CreateAndChaseCam<RecipeUnlockVFX>(centerPos);
            Craft.SharedModel.RecipeVFXSkipList.Add(recipeId);
            Craft.SharedModel.SaveRecipeVFXSkipList();

            _hiddenView.gameObject.SetActive(true);
            IsLocked = false;
            _unlockable = false;
        }

        public void SetSelected(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            selectedObject.SetActive(equals);
        }
    }
}
