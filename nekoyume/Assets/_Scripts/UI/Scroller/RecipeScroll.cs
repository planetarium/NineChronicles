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
using System.Text;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Module.Common;
using TMPro;

namespace Nekoyume.UI.Scroller
{
    using mixpanel;
    using State.Subjects;
    using Module;
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
        private GameObject eventScheduleTab;

        [SerializeField]
        private BlocksAndDatesPeriod eventScheduleTabEntireBlocksAndDatesPeriod;

        [SerializeField]
        private TextMeshProUGUI eventScheduleTabRemainingTimeText;

        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private TextMeshProUGUI emptyObjectText;

        [SerializeField]
        private GameObject openAllRecipeArea;

        [SerializeField]
        private Button openAllRecipeButton;

        [SerializeField]
        private TextMeshProUGUI openAllRecipeCostText;

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine;

        private BigInteger _openCost;

        private List<int> _unlockableRecipeIds = new List<int>();

        private List<IDisposable> _disposablesAtShow = new List<IDisposable>();

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
            Tracer.Trace("Unity/UnlockEquipmentRecipe", new Dictionary<string, string>()
            {
                ["BurntCrystal"] = _openCost.ToString(),
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });
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
            eventScheduleTab.SetActive(false);
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
            var items = Craft.SharedModel.EquipmentRecipeMap.Values
                .Where(x => x.ItemSubType == type)
                .ToList();
            emptyObjectText.text = L10nManager.Localize("UI_WORKSHOP_EMPTY_CATEGORY");
            emptyObject.SetActive(!items.Any());
            Show(items);

            var max = 0;
            foreach (var item in items)
            {
                if (item.Rows.Any(row => row is EquipmentItemRecipeSheet.Row equipmentRow
                                         && !IsEquipmentLocked(equipmentRow)))
                {
                    max = Mathf.Max(max, items.IndexOf(item));
                }
            }
            // `Scroller.ViewportSize`(viewport.rect.size.x,y) was not initialized.
            const float viewportSizeVertical = 458f;  // in `UI_Craft` prefab
            AdjustCellIntervalAndScrollOffset(viewportSizeVertical);
            JumpTo(max-1);

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
            openAllRecipeArea.SetActive(false);
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(true);
            eventScheduleTab.SetActive(false);
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
            var items = Craft.SharedModel.ConsumableRecipeMap.Values
                .Where(x => x.StatType == type)
                .ToList();
            emptyObjectText.text = L10nManager.Localize("UI_WORKSHOP_EMPTY_CATEGORY");
            emptyObject.SetActive(!items.Any());
            Show(items, true);
            AnimateScroller();

            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(_disposablesAtShow);
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
                .SubscribeTo(eventScheduleTabRemainingTimeText)
                .AddTo(_disposablesAtShow);

            openAllRecipeArea.SetActive(false);
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(false);

            if (RxProps.EventScheduleRowForRecipe is null ||
                RxProps.EventConsumableItemRecipeRows.Value?.Count is null or 0)
            {
                Show(new List<RecipeRow.Model>(), true);
                emptyObjectText.text = L10nManager.Localize("UI_EVENT_NOT_IN_PROGRESS");
                viewport.SetActive(false);
                emptyObject.SetActive(true);
                eventScheduleTab.SetActive(false);
                return;
            }

            var items = Craft.SharedModel.EventConsumableRecipeMap
                .Values.ToList();
            viewport.SetActive(true);
            emptyObject.SetActive(false);
            eventScheduleTab.SetActive(true);
            Show(items, true);
            AnimateScroller();
        }

        private void UpdateEventScheduleEntireTime(
            EventScheduleSheet.Row row)
        {
            if (row is null)
            {
                eventScheduleTabEntireBlocksAndDatesPeriod.Hide();
                return;
            }

            eventScheduleTabEntireBlocksAndDatesPeriod.Show(
                row.StartBlockIndex,
                row.RecipeEndBlockIndex,
                Game.Game.instance.Agent.BlockIndex,
                DateTime.UtcNow);
        }

        private void UpdateEventScheduleRemainingTime(
            EventScheduleSheet.Row row,
            long currentBlockIndex)
        {
            if (row is null)
            {
                eventScheduleTabRemainingTimeText.text = string.Empty;
                return;
            }

            var value = row.RecipeEndBlockIndex - currentBlockIndex;
            var time = value.BlockRangeToTimeSpanString();
            eventScheduleTabRemainingTimeText.text = $"{value}({time})";
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
                openAllRecipeCostText.color = hasEnoughBalance
                    ? Palette.GetColor(ColorType.ButtonEnabled)
                    : Palette.GetColor(ColorType.ButtonDisabled);
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
