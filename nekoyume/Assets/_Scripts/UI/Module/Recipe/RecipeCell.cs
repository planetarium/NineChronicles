using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.TableData;
using Nekoyume.Model.Item;
using Nekoyume.Game.Controller;
using System;

namespace Nekoyume.UI.Module
{
    using Nekoyume.Helper;
    using UniRx;

    public class RecipeCell : MonoBehaviour
    {
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private GameObject selectedObject = null;
        [SerializeField] private GameObject lockObject = null;
        [SerializeField] private Button button = null;

        private SheetRow<int> _recipeRow = null;
        private IDisposable _disposableForOnDisable = null;

        private void Awake()
        {
            if (button.interactable)
            {
                button.onClick.AddListener(() =>
                {
                    if (!lockObject.activeSelf)
                    {
                        AudioController.PlayClick();
                        Craft.SharedModel.SelectedRow.Value = _recipeRow;
                    }
                });
            }
        }

        private void OnDisable()
        {
            selectedObject.SetActive(false);
            _disposableForOnDisable?.Dispose();
        }

        public void Show(SheetRow<int> recipeRow)
        {
            _recipeRow = recipeRow;
            if (button.interactable)
            {
                var property = Craft.SharedModel.SelectedRow;

                SetSelected(property.Value);
                _disposableForOnDisable = property
                    .Subscribe(SetSelected);
            }

            var tableSheets = Game.Game.instance.TableSheets;

            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultItem();
                var viewData = recipeViewData.GetData(resultItem.Grade);
                equipmentView.Show(viewData, resultItem);
                consumableView.Hide();
            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultItem();
                var viewData = recipeViewData.GetData(resultItem.Grade);
                equipmentView.Hide();
                consumableView.Show(viewData, resultItem);
            }
            else
            {
                Debug.LogError($"Not supported type of recipe.");
            }

            lockObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Lock()
        {
            equipmentView.Hide();
            consumableView.Hide();
            lockObject.SetActive(true);
        }

        public void SetSelected(SheetRow<int> row)
        {
            var equals = ReferenceEquals(row, _recipeRow);
            selectedObject.SetActive(equals);
        }
    }
}
