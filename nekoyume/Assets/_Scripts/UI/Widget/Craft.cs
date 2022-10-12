using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System.Text.Json;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State.Subjects;
using Nekoyume.TableData.Event;
using NUnit.Framework;

namespace Nekoyume.UI
{
    using mixpanel;
    using UniRx;
    using Toggle = Module.Toggle;

    public class Craft : Widget
    {
        private static readonly int FirstClicked =
            Animator.StringToHash("FirstClicked");
        private static readonly int EquipmentClick =
            Animator.StringToHash("EquipmentClick");
        private static readonly int ConsumableClick =
            Animator.StringToHash("ConsumableClick");

        [SerializeField]
        private Toggle equipmentToggle;

        [SerializeField]
        private Toggle consumableToggle;

        [SerializeField]
        private Toggle eventConsumableToggle;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private RecipeScroll recipeScroll;

        [SerializeField]
        private SubRecipeView equipmentSubRecipeView;

        [SerializeField]
        private SubRecipeView consumableSubRecipeView;

        [SerializeField]
        private SubRecipeView eventConsumableSubRecipeView;

        [SerializeField]
        private CanvasGroup canvasGroup;

        public static RecipeModel SharedModel { get; set; }

        private readonly List<IDisposable> _disposablesAtShow = new();

        private const string ConsumableRecipeGroupPath = "Recipe/ConsumableRecipeGroup";

        private bool _isEquipment;

        private List<IDisposable> _disposables = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });

            CloseWidget = () =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            };

            equipmentToggle.OnValueChangedAsObservable()
                .DistinctUntilChanged()
                .Subscribe(OnClickEquipmentToggle)
                .AddTo(gameObject);
            consumableToggle.OnValueChangedAsObservable()
                .DistinctUntilChanged()
                .Subscribe(OnClickConsumableToggle)
                .AddTo(gameObject);
            eventConsumableToggle.OnValueChangedAsObservable()
                .DistinctUntilChanged()
                .Subscribe(OnClickConsumableToggle)
                .AddTo(gameObject);

            equipmentSubRecipeView.CombinationActionSubject
                .Subscribe(OnClickEquipmentAction)
                .AddTo(gameObject);

            consumableSubRecipeView.CombinationActionSubject
                .Subscribe(OnClickConsumableAction)
                .AddTo(gameObject);

            eventConsumableSubRecipeView.CombinationActionSubject
                .Subscribe(EventConsumableItemCraftsAction)
                .AddTo(gameObject);
        }

        protected override void OnDisable()
        {
            Animator.SetBool(FirstClicked, false);
            Animator.ResetTrigger(EquipmentClick);
            Animator.ResetTrigger(ConsumableClick);
            base.OnDisable();
        }

        public override void Initialize()
        {
            LoadRecipeModel();
            SharedModel.SelectedRow
                .Subscribe(SetSubRecipe)
                .AddTo(gameObject);
            SharedModel.UnlockedRecipes
                .Subscribe(list =>
                {
                    if (list != null &&
                        SharedModel.SelectedRow.Value != null &&
                        list.Contains(SharedModel.SelectedRow.Value.Key))
                    {
                        SetSubRecipe(SharedModel.SelectedRow.Value);
                    }
                })
                .AddTo(gameObject);

            ReactiveAvatarState.Address.Subscribe(address =>
            {
                if (address.Equals(default))
                {
                    return;
                }

                SharedModel.UpdateUnlockedRecipesAsync(address);
            }).AddTo(gameObject);

            ReactiveAvatarState.QuestList
                .Subscribe(SubscribeQuestList)
                .AddTo(gameObject);

            ReactiveAvatarState.Inventory
                .Subscribe(_ =>
                {
                    if (equipmentSubRecipeView.gameObject.activeSelf)
                    {
                        equipmentSubRecipeView.UpdateView();
                    }
                    else if (consumableSubRecipeView.gameObject.activeSelf)
                    {
                        consumableSubRecipeView.UpdateView();
                    }
                    else if (eventConsumableSubRecipeView.gameObject.activeSelf)
                    {
                        eventConsumableSubRecipeView.UpdateView();
                    }
                })
                .AddTo(gameObject);
        }

        public void ShowWithEquipmentRecipeId(
            int equipmentRecipeId,
            bool ignoreShowAnimation = false)
        {
            ShowWithToggleIndex(0, ignoreShowAnimation);

            if (!TableSheets.Instance.EquipmentItemRecipeSheet
                    .TryGetValue(equipmentRecipeId, out var row))
            {
                return;
            }

            var itemRow = row.GetResultEquipmentItemRow();
            recipeScroll.ShowAsEquipment(itemRow.ItemSubType, true);
            var group = RecipeModel.GetEquipmentGroup(row.ResultEquipmentId);
            recipeScroll.GoToRecipeGroup(group);
            if (SharedModel.UnlockedRecipes.Value.Contains(equipmentRecipeId))
            {
                SharedModel.SelectedRow.Value = row;
            }
        }

        public void ShowWithToggleIndex(int toggleIndex, bool ignoreShowAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            eventConsumableToggle.gameObject.SetActive(true);
            equipmentSubRecipeView.gameObject.SetActive(false);
            consumableSubRecipeView.gameObject.SetActive(false);
            eventConsumableSubRecipeView.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);

            // Toggles can be switched after enabled.
            switch (toggleIndex)
            {
                case 0:
                    equipmentToggle.isOn = true;
                    ShowEquipment();
                    break;
                case 1:
                    consumableToggle.isOn = true;
                    ShowConsumable();
                    AnimationState.Value = AnimationStateType.Shown;
                    break;
                case 2:
                    eventConsumableToggle.isOn = true;
                    ShowConsumable();
                    AnimationState.Value = AnimationStateType.Shown;
                    break;
            }

            if (!Game.Game.instance.Stage.TutorialController.IsPlaying)
            {
                HelpTooltip.HelpMe(100016, true);
            }

            if (!AudioController.instance.CurrentPlayingMusicName
                    .Equals(AudioController.MusicCode.Combination))
            {
                AudioController.instance
                    .PlayMusic(AudioController.MusicCode.Combination);
            }

            RxProps.EventScheduleRowForRecipe
                .Skip(1)
                .Select(_ => eventConsumableToggle.isOn)
                .Subscribe(OnClickConsumableToggle)
                .AddTo(_disposablesAtShow);
            RxProps.EventConsumableItemRecipeRows
                .Subscribe(value =>
                {
                    SharedModel.UpdateEventConsumable(value);
                    OnClickConsumableToggle(eventConsumableToggle.isOn);
                })
                .AddTo(_disposablesAtShow);
            HammerPointStatesSubject.HammerPointSubject.Subscribe(_ =>
            {
                if (equipmentSubRecipeView.gameObject.activeSelf)
                {
                    equipmentSubRecipeView.UpdateView();
                }
            }).AddTo(_disposablesAtShow);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowWithToggleIndex(0, ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesAtShow.DisposeAllAndClear();
            SharedModel.SelectedRow.Value = null;
            base.Close(ignoreCloseAnimation);
        }

        # region Invoke from animation

        private void ShowEquipment()
        {
            Assert.True(equipmentToggle.isOn);
            equipmentSubRecipeView.ResetSelectedIndex();
            recipeScroll.ShowAsEquipment(ItemSubType.Weapon, true);
            SharedModel.SelectedRow.Value = null;
        }

        private void ShowConsumable()
        {
            if (consumableToggle.isOn)
            {
                consumableSubRecipeView.ResetSelectedIndex();
                recipeScroll.ShowAsFood(StatType.HP, true);

            }
            else if (eventConsumableToggle.isOn)
            {
                eventConsumableSubRecipeView.ResetSelectedIndex();
                recipeScroll.ShowAsEventConsumable();
            }

            SharedModel.SelectedRow.Value = null;
        }

        # endregion Invoke from animation

        private void SetSubRecipe(SheetRow<int> row)
        {
            switch (row)
            {
                case EquipmentItemRecipeSheet.Row equipmentRow:
                    equipmentSubRecipeView.gameObject.SetActive(true);
                    consumableSubRecipeView.gameObject.SetActive(false);
                    eventConsumableSubRecipeView.gameObject.SetActive(false);
                    equipmentSubRecipeView.SetData(equipmentRow, equipmentRow.SubRecipeIds);
                    break;
                // NOTE: We must check the `row` is type of `EventConsumableItemRecipeSheet.Row`
                //       because the `EventConsumableItemRecipeSheet.Row` is the inherited class of
                //       the `ConsumableItemRecipeSheet.Row`.
                case EventConsumableItemRecipeSheet.Row eventConsumableRow:
                    equipmentSubRecipeView.gameObject.SetActive(false);
                    consumableSubRecipeView.gameObject.SetActive(false);
                    eventConsumableSubRecipeView.gameObject.SetActive(true);
                    eventConsumableSubRecipeView.SetData(eventConsumableRow, null);
                    break;
                case ConsumableItemRecipeSheet.Row consumableRow:
                    equipmentSubRecipeView.gameObject.SetActive(false);
                    consumableSubRecipeView.gameObject.SetActive(true);
                    eventConsumableSubRecipeView.gameObject.SetActive(false);
                    consumableSubRecipeView.SetData(consumableRow, null);
                    break;
                default:
                    equipmentSubRecipeView.gameObject.SetActive(false);
                    consumableSubRecipeView.gameObject.SetActive(false);
                    eventConsumableSubRecipeView.gameObject.SetActive(false);
                    break;
            }
        }

        private static void LoadRecipeModel()
        {
            var jsonAsset = Resources.Load<TextAsset>(ConsumableRecipeGroupPath);
            var group = jsonAsset is null
                ? null
                : JsonSerializer.Deserialize<CombinationRecipeGroup>(jsonAsset.text);

            SharedModel = new RecipeModel(
                TableSheets.Instance.EquipmentItemRecipeSheet.Values,
                group?.Groups ?? Array.Empty<RecipeGroup>(),
                RxProps.EventConsumableItemRecipeRows.Value);
        }

        private static void SubscribeQuestList(QuestList questList)
        {
            var quest = questList?
                .OfType<CombinationEquipmentQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.StageId)
                .FirstOrDefault();

            if (quest is null ||
                !TableSheets.Instance.EquipmentItemRecipeSheet
                    .TryGetValue(quest.RecipeId, out var row) ||
                !States.Instance.CurrentAvatarState.worldInformation
                    .TryGetLastClearedStageId(out var clearedStage))
            {
                SharedModel.NotifiedRow.Value = null;
                return;
            }

            var stageId = row.UnlockStage;
            SharedModel.NotifiedRow.Value = clearedStage >= stageId
                ? row
                : null;
        }

        private void OnClickEquipmentToggle(bool value)
        {
            AudioController.PlayClick();

            if (!value)
            {
                return;
            }

            if (Animator.GetBool(FirstClicked))
            {
                Animator.SetTrigger(EquipmentClick);
            }
        }

        private void OnClickConsumableToggle(bool value)
        {
            AudioController.PlayClick();
            if (!value)
            {
                return;
            }

            if (Animator.GetBool(FirstClicked))
            {
                if (Animator.GetCurrentAnimatorStateInfo(0)
                    .IsName("Consumable"))
                {
                    ShowConsumable();
                }
                else
                {
                    Animator.SetTrigger(ConsumableClick);
                }
            }
            else
            {
                Animator.SetBool(FirstClicked, true);
            }
        }

        private void OnClickEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            // NOTE: Is these lines necessary?
            // var requirementSheet = TableSheets.Instance.ItemRequirementSheet;
            // var recipeSheet = TableSheets.Instance.EquipmentItemRecipeSheet;
            // if (!recipeSheet.TryGetValue(recipeInfo.RecipeId, out var recipeRow))
            // {
            //     return;
            // }
            //
            // var resultItemRow = recipeRow.GetResultEquipmentItemRow();
            // if (!requirementSheet.TryGetValue(resultItemRow.Id, out _))
            // {
            //     CombinationEquipmentAction(recipeInfo);
            //     return;
            // }

            CombinationEquipmentAction(recipeInfo);
        }

        private void OnClickConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            // NOTE: Is these lines necessary?
            // var requirementSheet = TableSheets.Instance.ItemRequirementSheet;
            // var recipeSheet = TableSheets.Instance.ConsumableItemRecipeSheet;
            // if (!recipeSheet.TryGetValue(recipeInfo.RecipeId, out var recipeRow))
            // {
            //     return;
            // }
            //
            // var resultItemRow = recipeRow.GetResultConsumableItemRow();
            // if (!requirementSheet.TryGetValue(resultItemRow.Id, out _))
            // {
            //     CombinationConsumableAction(recipeInfo);
            //     return;
            // }

            CombinationConsumableAction(recipeInfo);
        }

        private void CombinationEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!equipmentSubRecipeView.CheckSubmittable(
                    out var errorMessage,
                    out var slotIndex))
            {
                OneLineSystem.Push(
                    MailType.System,
                    errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var tableSheets = TableSheets.Instance;
            var equipmentRow = tableSheets.EquipmentItemRecipeSheet[recipeInfo.RecipeId];
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow.GetResultEquipmentItemRow(),
                Guid.Empty,
                default);
            var requiredBlockIndex = equipmentRow.RequiredBlockIndex;
            if (recipeInfo.SubRecipeId.HasValue)
            {
                var subRecipeRow =
                    tableSheets.EquipmentItemSubRecipeSheetV2[recipeInfo.SubRecipeId.Value];
                requiredBlockIndex += subRecipeRow.RequiredBlockIndex;
            }

            equipmentSubRecipeView.UpdateView();
            var insufficientMaterials = recipeInfo.ReplacedMaterials;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (insufficientMaterials.Any())
            {
                Find<ReplaceMaterialPopup>().Show(insufficientMaterials,
                    () =>
                    {
                        var slots = Find<CombinationSlotsPopup>();
                        slots.SetCaching(
                            avatarAddress,
                            slotIndex,
                            true,
                            requiredBlockIndex,
                            itemUsable: equipment);
                        Find<HeaderMenuStatic>().Crystal.SetProgressCircle(true);

                        Analyzer.Instance.Track(
                            "Unity/Replace Combination Material",
                            new Value
                            {
                                ["MaterialCount"] = insufficientMaterials
                                    .Sum(x => x.Value),
                                ["BurntCrystal"] = (long)recipeInfo.CostCrystal,
                                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                            });

                        ActionManager.Instance
                            .CombinationEquipment(
                                recipeInfo,
                                slotIndex,
                                true,
                                false)
                            .Subscribe();
                        StartCoroutine(CoCombineNPCAnimation(equipment, requiredBlockIndex));
                    });
            }
            else
            {
                var slots = Find<CombinationSlotsPopup>();
                slots.SetCaching(
                    avatarAddress,
                    slotIndex,
                    true,
                    requiredBlockIndex,
                    itemUsable: equipment);
                ActionManager.Instance
                    .CombinationEquipment(
                        recipeInfo,
                        slotIndex,
                        false,
                        false)
                    .Subscribe();
                StartCoroutine(CoCombineNPCAnimation(equipment, requiredBlockIndex));
            }
        }

        private void CombinationConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!consumableSubRecipeView.CheckSubmittable(
                    out var errorMessage,
                    out var slotIndex))
            {
                OneLineSystem.Push(
                    MailType.System,
                    errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var consumableRow = TableSheets.Instance
                .ConsumableItemRecipeSheet[recipeInfo.RecipeId];
            var consumable = (Consumable)ItemFactory.CreateItemUsable(
                consumableRow.GetResultConsumableItemRow(),
                Guid.Empty,
                default);
            var requiredBlockIndex = consumableRow.RequiredBlockIndex;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var slots = Find<CombinationSlotsPopup>();
            slots.SetCaching(avatarAddress, slotIndex, true, requiredBlockIndex, itemUsable: consumable);

            consumableSubRecipeView.UpdateView();
            ActionManager.Instance.CombinationConsumable(recipeInfo, slotIndex).Subscribe();

            StartCoroutine(CoCombineNPCAnimation(consumable, requiredBlockIndex, true));
        }

        private void EventConsumableItemCraftsAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!eventConsumableSubRecipeView.CheckSubmittable(
                    out var errorMessage,
                    out var slotIndex))
            {
                OneLineSystem.Push(
                    MailType.System,
                    errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var consumableRow = TableSheets.Instance
                .EventConsumableItemRecipeSheet[recipeInfo.RecipeId];
            var consumable = (Consumable)ItemFactory.CreateItemUsable(
                consumableRow.GetResultConsumableItemRow(),
                Guid.Empty,
                default);
            var requiredBlockIndex = consumableRow.RequiredBlockIndex;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var slots = Find<CombinationSlotsPopup>();
            slots.SetCaching(
                avatarAddress,
                slotIndex,
                true,
                requiredBlockIndex,
                itemUsable: consumable);

            eventConsumableSubRecipeView.UpdateView();
            ActionManager.Instance
                .EventConsumableItemCrafts(
                    RxProps.EventScheduleRowForRecipe.Value.Id,
                    recipeInfo,
                    slotIndex)
                .Subscribe();

            StartCoroutine(CoCombineNPCAnimation(
                consumable,
                requiredBlockIndex,
                true));
        }

        private IEnumerator CoCombineNPCAnimation(
            ItemBase itemBase,
            long blockIndex,
            bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(null);
            loadingScreen.OnDisappear = OnNPCDisappear;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(itemBase.ItemType, quote);
        }

        private void OnNPCDisappear()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Pop();
        }

        public void TutorialActionClickFirstRecipeCellView()
        {
            SharedModel.SelectedRow.Value = SharedModel.RecipeForTutorial;
            SharedModel.SelectedRecipeCell.UnlockDummyLocked();
        }

        public void TutorialActionClickCombinationSubmitButton()
        {
            equipmentSubRecipeView.CombineCurrentRecipe();
        }

        public void TutorialActionCloseCombination()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Close(true);
            Game.Event.OnRoomEnter.Invoke(true);
        }
    }
}
