using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Nekoyume.UI.Scroller
{
    using Nekoyume.L10n;
    using Nekoyume.State;
    using Nekoyume.UI.Module;
    using System.Numerics;
    using UniRx;

    public class RecipeScroll : RectScroll<RecipeRow.Model, RecipeScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [Serializable]
        private struct EquipmentCategoryToggle
        {
            public Toggle Toggle;
            public ItemSubType Type;
            public Image IndicatorImage;
        }

        [Serializable]
        private struct ConsumableCategoryToggle
        {
            public Toggle Toggle;
            public StatType Type;
        }

        [SerializeField]
        private List<EquipmentCategoryToggle> equipmentCategoryToggles = null;

        [SerializeField]
        private List<ConsumableCategoryToggle> consumableCategoryToggles = null;

        [SerializeField]
        private GameObject equipmentTab = null;

        [SerializeField]
        private GameObject consumableTab = null;

        [SerializeField]
        private GameObject emptyObject = null;

        [SerializeField]
        private GameObject openAllRecipeArea = null;

        [SerializeField]
        private Button openAllRecipeButton = null;

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine = null;

        private List<int> _unlockableRecipes = null;

        private BigInteger _openCost;

        private ItemSubType _displayingItemSubType;

        private List<IDisposable> _disposablesOnDisabled = new List<IDisposable>();

        protected void Awake()
        {
            foreach (var categoryToggle in equipmentCategoryToggles)
            {
                var type = categoryToggle.Type;
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ShowAsEquipment(type);
                });
            }

            foreach (var categoryToggle in consumableCategoryToggles)
            {
                var type = categoryToggle.Type;
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ShowAsFood(type);
                });
            }

            openAllRecipeButton.onClick.AddListener(OpenEveryAvailableRecipes);
        }

        private void OnDisable()
        {
            _disposablesOnDisabled.DisposeAllAndClear();
        }

        private void OpenEveryAvailableRecipes()
        {
            var usageMessage = L10nManager.Localize("UI_UNLOCK_RECIPES_FORMAT", _unlockableRecipes.Count);
            Widget.Find<PaymentPopup>().Show(
                ReactiveAvatarState.CrystalBalance,
                _openCost,
                usageMessage,
                UnlockRecipeAction);
        }

        private void UnlockRecipeAction()
        {
            var sharedModel = Craft.SharedModel;

            sharedModel.UnlockingRecipes.AddRange(_unlockableRecipes);
            var cells = GetComponentsInChildren<RecipeCell>();
            foreach (var cell in cells)
            {
                if (_unlockableRecipes.Contains(cell.RecipeId))
                {
                    cell.Unlock();
                }
            }

            LocalLayerModifier.ModifyAvatarCrystal(
                States.Instance.CurrentAvatarState.address, -_openCost);
            Game.Game.instance.ActionManager
                .UnlockEquipmentRecipe(_unlockableRecipes)
                .Subscribe();
        }

        public void ShowAsEquipment(ItemSubType type, bool updateToggle = false)
        {
            _displayingItemSubType = type;
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(true);
            consumableTab.SetActive(false);
            if (updateToggle)
            {
                var toggle = equipmentCategoryToggles.Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsEquipment(type);
                    return;
                }
                toggle.Toggle.isOn = true;
                return;
            }

            var items = Craft.SharedModel.EquipmentRecipeMap.Values
                .Where(x => x.ItemSubType == type)
                ?? Enumerable.Empty<RecipeRow.Model>();

            emptyObject.SetActive(!items.Any());
            Show(items, true);
            AnimateScroller();

            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesOnDisabled);
            Craft.SharedModel.UnlockedRecipes
                .Subscribe(UpdateUnlockAllButton)
                .AddTo(_disposablesOnDisabled);

        }

        private void UpdateUnlockAllButton(List<int> unlockedRecipes)
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(out var lastClearedStageId))
            {
                openAllRecipeArea.SetActive(false);
            }
            else
            {
                var sheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
                var availableRecipes = sheet.Values
                    .Where(x =>
                        x.GetResultEquipmentItemRow().ItemSubType == _displayingItemSubType &&
                        x.UnlockStage <= lastClearedStageId &&
                        !unlockedRecipes.Contains(x.Id))
                    .OrderBy(x => x.UnlockStage);

                _unlockableRecipes = new List<int>();
                var balance = ReactiveAvatarState.CrystalBalance.MajorUnit;
                _openCost = 0;
                foreach (var availableRecipe in availableRecipes)
                {
                    if (_openCost + availableRecipe.CRYSTAL > balance)
                    {
                        break;
                    }

                    _openCost += availableRecipe.CRYSTAL;
                    _unlockableRecipes.Add(availableRecipe.Id);
                }
                openAllRecipeArea.SetActive(_unlockableRecipes.Count() >= 2);
            }
        }

        public void ShowAsFood(StatType type, bool updateToggle = false)
        {
            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesOnDisabled);

            openAllRecipeArea.SetActive(false);
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(true);
            if (updateToggle)
            {
                var toggle = consumableCategoryToggles.Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsFood(type);
                    return;
                }
                toggle.Toggle.isOn = true;
                return;
            }

            var items = Craft.SharedModel.ConsumableRecipeMap.Values
                .Where(x => x.StatType == type)
                ?? Enumerable.Empty<RecipeRow.Model>();

            emptyObject.SetActive(!items.Any());
            Show(items, true);
            AnimateScroller();
        }

        private void AnimateScroller()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(CoAnimateScroller());
        }

        private IEnumerator CoAnimateScroller()
        {
            var rows = GetComponentsInChildren<RecipeRow>();
            var wait = new WaitForSeconds(animationInterval);

            Scroller.Draggable = false;
            foreach (var row in rows)
            {
                row.HideWithAlpha();
            }

            yield return null;
            Relayout();

            foreach (var row in rows)
            {
                row.ShowAnimation();
                yield return wait;
            }
            Scroller.Draggable = true;

            _animationCoroutine = null;
        }

        public void SubscribeNotifiedRow(SheetRow<int> row)
        {
            if (!(row is EquipmentItemRecipeSheet.Row equipmentRow))
            {
                foreach (var toggle in equipmentCategoryToggles)
                {
                    toggle.IndicatorImage.enabled = false;
                }

                return;
            }

            var resultItem = equipmentRow.GetResultEquipmentItemRow();
            foreach (var toggle in equipmentCategoryToggles)
            {
                toggle.IndicatorImage.enabled =
                    toggle.Type == resultItem.ItemSubType;
            }
        }

        public void GoToRecipeGroup(string equipmentRecipeGroup)
        {
            if (!Craft.SharedModel.EquipmentRecipeMap
                .TryGetValue(equipmentRecipeGroup, out var model))
            {
                return;
            }

            JumpTo(model);
        }
    }
}
