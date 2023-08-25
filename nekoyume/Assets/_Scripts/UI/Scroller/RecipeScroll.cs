using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    using State.Subjects;
    using Module;
    using UniRx;

    public class RecipeScroll : GridScroll<SheetRow<int>, RecipeScroll.ContextModel, RecipeScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
        }

        public class CellCellGroup : GridCellGroup<SheetRow<int>, ContextModel>
        {
        }

        private enum Filter
        {
            Name,
            Level,
            Grade,
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

        [Serializable]
        private struct EventScheduleTab
        {
            public GameObject container;
            public BlocksAndDatesPeriod blocksAndDatesPeriod;
            public TextMeshProUGUI remainingTimeText;
        }

        [Serializable]
        private struct OpenAllRecipeArea
        {
            public GameObject container;
            public Button button;
            public TextMeshProUGUI costText;
        }

        [Serializable]
        private struct SortArea
        {
            public GameObject container;
            public TMP_Dropdown filter;
            public Button button;
            public UIFlip buttonArrow;
        }

        [SerializeField]
        private RecipeCell cellTemplate = null;

        [SerializeField]
        private GameObject viewport;

        [SerializeField]
        private List<EquipmentCategoryToggle> equipmentCategoryToggles;

        [SerializeField]
        private List<ConsumableCategoryToggle> consumableCategoryToggles;

        [SerializeField]
        private GameObject equipmentTab;

        [SerializeField]
        private GameObject consumableTab;

        [SerializeField]
        private EventScheduleTab eventScheduleTab;

        [SerializeField]
        private SortArea sortArea;

        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private TextMeshProUGUI emptyObjectText;

        [SerializeField]
        private OpenAllRecipeArea openAllRecipeArea;

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine;

        private BigInteger _openCost;

        private List<int> _unlockableRecipeIds = new ();

        private List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        private readonly ReactiveProperty<Filter> _selectedFilter = new();
        private readonly ReactiveProperty<bool> _isAscending = new();

        protected override FancyCell<SheetRow<int>, ContextModel> CellTemplate => cellTemplate;

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

            var options = Enum.GetNames(typeof(Filter)).Select(f => f.ToString()).ToList();
            sortArea.filter.AddOptions(options);
            sortArea.filter.onValueChanged.AddListener(index =>
            {
                _selectedFilter.Value = (Filter)index;
                AudioController.PlayClick();
            });
            sortArea.button.onClick.AddListener(() =>
            {
                _isAscending.Value = !_isAscending.Value;
                AudioController.PlayClick();
            });

            _selectedFilter.Value = Filter.Level;
            _isAscending.Value = true;

            _selectedFilter.Subscribe(filter =>
            {
                _isAscending.SetValueAndForceNotify(true);
            });
            _isAscending.Subscribe(ascending =>
            {
                sortArea.buttonArrow.vertical = !_isAscending.Value;
                // Todo : Sort with selected filter, ascending.
                Debug.LogError($"Sort with {_selectedFilter.Value}, ascending: {_isAscending.Value}");
            });

            openAllRecipeArea.button.onClick.AddListener(OpenEveryAvailableRecipes);
        }

        private void OnDisable()
        {
            _disposablesAtShow.DisposeAllAndClear();
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
                Widget.Find<PaymentPopup>().ShowAttract(
                    CostType.Crystal,
                    _openCost,
                    message,
                    L10nManager.Localize("UI_GO_GRINDING"),
                    onAttract);
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

            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(true);
            Game.Game.instance.ActionManager
                .UnlockEquipmentRecipe(_unlockableRecipeIds, _openCost)
                .Subscribe();
            UpdateUnlockAllButton();
        }

        public void ShowAsEquipment(ItemSubType type, bool updateToggle = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            Craft.SharedModel.DisplayingItemSubtype = type;
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(true);
            consumableTab.SetActive(false);
            eventScheduleTab.container.SetActive(false);
            if (updateToggle)
            {
                var toggle = equipmentCategoryToggles
                    .Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsEquipment(type);
                    return;
                }

                toggle.Toggle.isOn = true;
                return;
            }

            viewport.SetActive(true);
            var items = Craft.SharedModel.EquipmentRecipeMap[type];
            emptyObjectText.text = L10nManager.Localize("UI_WORKSHOP_EMPTY_CATEGORY");
            emptyObject.SetActive(!items.Any());
            sortArea.container.SetActive(items.Any());
            Show(items);

            var max = items.Last(row =>
                row is EquipmentItemRecipeSheet.Row equipmentRow &&
                !IsEquipmentLocked(equipmentRow));
            JumpTo(max);

            AnimateScroller();

            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesAtShow);
            Craft.SharedModel.UnlockedRecipes
                .Subscribe(_ => UpdateUnlockAllButton())
                .AddTo(_disposablesAtShow);
            AgentStateSubject.Crystal
                .Subscribe(_ => UpdateUnlockAllButton())
                .AddTo(_disposablesAtShow);
        }

        private static bool IsEquipmentLocked(EquipmentItemRecipeSheet.Row equipmentRow)
        {
            var worldInformation = States.Instance.CurrentAvatarState.worldInformation;
            var clearedStage = worldInformation.TryGetLastClearedStageId(out var stageId)
                ? stageId
                : 0;
            var sharedModel = Craft.SharedModel;

            if (equipmentRow.CRYSTAL == 0)
            {
                return false;
            }
            if (equipmentRow.UnlockStage - clearedStage > 0)
            {
                return true;
            }
            if (sharedModel.DummyLockedRecipes.Contains(equipmentRow.Id))
            {
                return true;
            }
            if (sharedModel.UnlockedRecipes is null)
            {
                return true;
            }
            if (sharedModel.UnlockingRecipes.Contains(equipmentRow.Id))
            {
                return false;
            }
            return !sharedModel.UnlockedRecipes.Value.Contains(equipmentRow.Id);
        }

        public void ShowAsFood(StatType type, bool updateToggle = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            openAllRecipeArea.container.SetActive(false);
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(true);
            eventScheduleTab.container.SetActive(false);
            if (updateToggle)
            {
                var toggle = consumableCategoryToggles
                    .Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsFood(type);
                    return;
                }

                toggle.Toggle.isOn = true;
                return;
            }

            viewport.SetActive(true);
            var items = Craft.SharedModel.ConsumableRecipeMap[type];
            emptyObjectText.text = L10nManager.Localize("UI_WORKSHOP_EMPTY_CATEGORY");
            emptyObject.SetActive(!items.Any());
            sortArea.container.SetActive(items.Any());
            Show(items);

            var max = items.Last(row =>
                row is ConsumableItemRecipeSheet.Row consumableRow &&
                !IsConsumableLocked(consumableRow));
            JumpTo(max);

            AnimateScroller();

            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesAtShow);
        }

        private static bool IsConsumableLocked(ConsumableItemRecipeSheet.Row consumableRow)
        {
            var sheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
            if (!sheet.TryGetValue(consumableRow.ResultConsumableItemId, out var requirementRow))
            {
                return true;
            }

            return States.Instance.CurrentAvatarState.level < requirementRow.Level;
        }

        public void ShowAsEventConsumable()
        {
            _disposablesAtShow.DisposeAllAndClear();
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(value => UpdateEventScheduleRemainingTime(
                    RxProps.EventScheduleRowForRecipe.Value,
                    value))
                .AddTo(_disposablesAtShow);
            RxProps.EventScheduleRowForRecipe
                .Subscribe(UpdateEventScheduleEntireTime)
                .AddTo(_disposablesAtShow);
            RxProps.EventRecipeRemainingTimeText
                .SubscribeTo(eventScheduleTab.remainingTimeText)
                .AddTo(_disposablesAtShow);

            openAllRecipeArea.container.SetActive(false);
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(false);

            if (RxProps.EventScheduleRowForRecipe is not null &&
                RxProps.EventConsumableItemRecipeRows.Value?.Count is not (null or 0))
            {
                var items = Craft.SharedModel.EventConsumableRecipeMap;
                viewport.SetActive(true);
                emptyObject.SetActive(false);
                sortArea.container.SetActive(true);
                eventScheduleTab.container.SetActive(true);
                Show(items, true);
                AnimateScroller();
            }
            else if (RxProps.EventScheduleRowForRecipe is not null &&
                     RxProps.EventMaterialItemRecipeRows.Value?.Count is not (null or 0))
            {
                var items = Craft.SharedModel.EventMaterialRecipeMap;
                viewport.SetActive(true);
                emptyObject.SetActive(false);
                sortArea.container.SetActive(true);
                eventScheduleTab.container.SetActive(true);
                Show(items, true);
                AnimateScroller();
            }
            else
            {
                Show(new List<SheetRow<int>>(), true);
                emptyObjectText.text = L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS");
                viewport.SetActive(false);
                emptyObject.SetActive(true);
                sortArea.container.SetActive(false);
                eventScheduleTab.container.SetActive(false);
            }
        }

        private void UpdateEventScheduleEntireTime(EventScheduleSheet.Row row)
        {
            if (row is null)
            {
                eventScheduleTab.blocksAndDatesPeriod.Hide();
                return;
            }

            eventScheduleTab.blocksAndDatesPeriod.Show(
                row.StartBlockIndex,
                row.RecipeEndBlockIndex,
                Game.Game.instance.Agent.BlockIndex,
                LiveAssetManager.instance.GameConfig.SecondsPerBlock,
                DateTime.UtcNow);
        }

        private void UpdateEventScheduleRemainingTime(
            EventScheduleSheet.Row row,
            long currentBlockIndex)
        {
            if (row is null)
            {
                eventScheduleTab.remainingTimeText.text = string.Empty;
                return;
            }

            var value = row.RecipeEndBlockIndex - currentBlockIndex;
            var time = value.BlockRangeToTimeSpanString();
            eventScheduleTab.remainingTimeText.text = $"{value}({time})";
        }

        private void UpdateUnlockAllButton()
        {
            Craft.SharedModel.UpdateUnlockableRecipes();
            _unlockableRecipeIds = Craft.SharedModel.UnlockableRecipes.Value;
            _openCost = Craft.SharedModel.UnlockableRecipesOpenCost;

            var isActive = _unlockableRecipeIds.Any();
            openAllRecipeArea.container.SetActive(isActive);
            if (isActive)
            {
                openAllRecipeArea.costText.text = _openCost.ToString();

                var hasEnoughBalance = States.Instance.CrystalBalance.MajorUnit >= _openCost;
                openAllRecipeArea.costText.color = hasEnoughBalance
                    ? Palette.GetColor(ColorType.ButtonEnabled)
                    : Palette.GetColor(ColorType.ButtonDisabled);
            }
        }

        private void AnimateScroller()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
                var rows = GetComponentsInChildren<RecipeCell>(true);
                foreach (var row in rows)
                {
                    row.ShowWithAlpha(true);
                }
            }

            _animationCoroutine = StartCoroutine(CoAnimateScroller());
        }

        private IEnumerator CoAnimateScroller()
        {
            Scroller.Draggable = false;

            yield return null;
            Relayout();

            var rows = GetComponentsInChildren<RecipeCell>();
            var wait = new WaitForSeconds(animationInterval);

            foreach (var row in rows)
            {
                row.HideWithAlpha();
            }

            yield return null;

            foreach (var row in rows)
            {
                row.ShowWithAlpha();
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
