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
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.State;
using System.Numerics;
using TMPro;

namespace Nekoyume.UI.Scroller
{
    using Nekoyume.State.Subjects;
    using Nekoyume.UI.Module;
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
        private TextMeshProUGUI openAllRecipeCostText = null;

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine = null;

        private BigInteger _openCost;

        private List<int> _unlockableRecipeIds = new List<int>();

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
            System.Action onAttract = () =>
            {
                Widget.Find<Craft>().Close(true);
                Widget.Find<Grind>().Show();
            };

            if (States.Instance.CrystalBalance.MajorUnit >= _openCost)
            {
                var usageMessage = L10nManager.Localize("UI_UNLOCK_RECIPES_FORMAT", _unlockableRecipeIds.Count);
                var balance = States.Instance.CrystalBalance;

                Widget.Find<PaymentPopup>().Show(
                    CostType.Crystal,
                    balance.MajorUnit,
                    _openCost,
                    balance.GetPaymentFormatText(usageMessage, _openCost),
                    L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                    UnlockRecipeAction,
                    onAttract);
            }
            else
            {
                var message = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                Widget.Find<PaymentPopup>().ShowAttract(_openCost, message, onAttract);
            }
        }

        private void UnlockRecipeAction()
        {
            var sharedModel = Craft.SharedModel;

            sharedModel.UnlockingRecipes.AddRange(_unlockableRecipeIds);
            var cells = GetComponentsInChildren<RecipeCell>();
            foreach (var cell in cells)
            {
                if (_unlockableRecipeIds.Contains(cell.RecipeId))
                {
                    cell.Unlock();
                }
            }

            Game.Game.instance.ActionManager
                .UnlockEquipmentRecipe(_unlockableRecipeIds, _openCost)
                .Subscribe();
            UpdateUnlockAllButton();
        }

        public void ShowAsEquipment(ItemSubType type, bool updateToggle = false)
        {
            _disposablesOnDisabled.DisposeAllAndClear();
            Craft.SharedModel.DisplayingItemSubtype = type;
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
                .Subscribe(_ => UpdateUnlockAllButton())
                .AddTo(_disposablesOnDisabled);
            AgentStateSubject.Crystal
                .Subscribe(_ => UpdateUnlockAllButton())
                .AddTo(_disposablesOnDisabled);
        }

        public void ShowAsFood(StatType type, bool updateToggle = false)
        {
            _disposablesOnDisabled.DisposeAllAndClear();
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


            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesOnDisabled);
        }

        private void UpdateUnlockAllButton()
        {
            Craft.SharedModel.UpdateUnlockableRecipes();
            _unlockableRecipeIds = Craft.SharedModel.UnlockableRecipes.Value;
            _openCost = Craft.SharedModel.UnlockableRecipesOpenCost;

            var isActive = _unlockableRecipeIds.Any();
            openAllRecipeArea.SetActive(isActive);
            if (isActive)
            {
                openAllRecipeCostText.text = _openCost.ToString();

                var hasEnoughBalance = States.Instance.CrystalBalance.MajorUnit >= _openCost;
                openAllRecipeCostText.color = hasEnoughBalance ?
                    Palette.GetColor(ColorType.ButtonEnabled) :
                    Palette.GetColor(ColorType.ButtonDisabled);
            }
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
