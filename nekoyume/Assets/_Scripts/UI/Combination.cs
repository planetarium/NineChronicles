using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;
using Nekoyume.Game.VFX;

namespace Nekoyume.UI
{
    public class Combination : Widget, LegacyRecipeCellView.IEventListener
    {
        public enum StateType
        {
            SelectMenu,
            CombineEquipment,
            CombineConsumable,
            LegacyCombineConsumable,
            EnhanceEquipment,
            CombinationConfirm,
        }

        [Serializable]
        public struct SelectionArea
        {
            public GameObject root;
            public CategoryButton combineEquipmentButton;
            public CategoryButton combineConsumableButton;
            public CategoryButton enhanceEquipmentButton;
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.SelectMenu);
        
        private const int NPCId = 300001;

        public SelectionArea selectionArea;

        private ToggleGroup _toggleGroup;
        public CategoryButton combineEquipmentCategoryButton;
        public CategoryButton combineConsumableCategoryButton;
        public CategoryButton enhanceEquipmentCategoryButton;

        public GameObject leftArea;
        public GameObject categoryTabArea;
        public EquipmentRecipe equipmentRecipe;
        public ConsumableRecipe consumableRecipe;

        public Module.Inventory inventory;

        public CombineConsumable combineConsumable;
        public EnhanceEquipment enhanceEquipment;
        public EquipmentCombinationPanel equipmentCombinationPanel;
        public ElementalCombinationPanel elementalCombinationPanel;
        public ConsumableCombinationPanel consumableCombinationPanel;
        public Recipe recipe;
        public SpeechBubble speechBubbleForEquipment;
        public SpeechBubble speechBubbleForUpgrade;
        public Transform npcPosition01;
        public Transform npcPosition02;
        public CanvasGroup canvasGroup;
        public Animator recipeAnimator;
        public ModuleBlur blur;
        public RecipeCellView selectedRecipe;

        public RecipeClickVFX recipeClickVFX;

        private NPC _npc01;
        private NPC _npc02;
        public int selectedIndex;
        private bool _lockSlotIndex;
        private long _blockIndex;
        private Dictionary<int, CombinationSlotState> _states;
        private SpeechBubble _selectedSpeechBubble;

        #region Override

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () => { };
        }

        public override void Initialize()
        {
            base.Initialize();

            selectionArea.combineEquipmentButton.OnClick
                .Subscribe(_ => State.SetValueAndForceNotify(StateType.CombineEquipment))
                .AddTo(gameObject);
            selectionArea.combineConsumableButton.OnClick
                .Subscribe(_ => State.SetValueAndForceNotify(StateType.CombineConsumable))
                .AddTo(gameObject);
            selectionArea.enhanceEquipmentButton.OnClick
                .Subscribe(_ => State.SetValueAndForceNotify(StateType.EnhanceEquipment))
                .AddTo(gameObject);

            selectionArea.combineEquipmentButton.SetLockCondition(GameConfig
                .RequireClearedStageLevel.CombinationEquipmentAction);
            selectionArea.combineConsumableButton.SetLockCondition(GameConfig
                .RequireClearedStageLevel.CombinationConsumableAction);
            selectionArea.enhanceEquipmentButton.SetLockCondition(GameConfig
                .RequireClearedStageLevel.ItemEnhancementAction);

            _toggleGroup = new ToggleGroup();
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(combineEquipmentCategoryButton);
            _toggleGroup.RegisterToggleable(combineConsumableCategoryButton);
            _toggleGroup.RegisterToggleable(enhanceEquipmentCategoryButton);

            combineEquipmentCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .CombinationEquipmentAction);
            combineConsumableCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .CombinationConsumableAction);
            enhanceEquipmentCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .ItemEnhancementAction);

            State.Subscribe(SubscribeState).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView.Subscribe(StageMaterial).AddTo(gameObject);

            combineConsumable.RemoveMaterialsAll();
            combineConsumable.OnMaterialChange.Subscribe(SubscribeOnMaterialChange)
                .AddTo(gameObject);
            combineConsumable.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionLegacyCombineConsumable();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);
            combineConsumable.recipeButton.OnClickAsObservable().Subscribe(_ =>
            {
                combineConsumable.submitButton.gameObject.SetActive(false);
                recipe.Show();
            }).AddTo(gameObject);

            enhanceEquipment.RemoveMaterialsAll();
            enhanceEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange)
                .AddTo(gameObject);
            enhanceEquipment.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionEnhanceEquipment();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            equipmentCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionEnhancedCombinationEquipment(equipmentCombinationPanel);
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            equipmentCombinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

            elementalCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionEnhancedCombinationEquipment(elementalCombinationPanel);
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            consumableCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionCombineConsumable();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            elementalCombinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

            recipe.RegisterListener(this);
            recipe.closeButton.OnClickAsObservable()
                .Subscribe(_ => combineConsumable.submitButton.gameObject.SetActive(true))
                .AddTo(gameObject);

            blur.gameObject.SetActive(false);

            CombinationSlotStatesSubject.CombinationSlotStates.Subscribe(SubscribeSlotStates)
                .AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            CheckLockOfCategoryButtons();

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("combination");
            var player = stage.GetPlayer();
            player.gameObject.SetActive(false);

            State.SetValueAndForceNotify(StateType.SelectMenu);

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character,
                BottomMenu.ToggleableType.Inventory,
                BottomMenu.ToggleableType.Combination
            );

            if (_npc01 is null)
            {
                var go = Game.Game.instance.Stage.npcFactory.Create(NPCId, npcPosition01.position);
                _npc01 = go.GetComponent<NPC>();
            }

            ShowSpeech("SPEECH_COMBINE_GREETING_", CharacterAnimation.Type.Greeting);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public void Show(int slotIndex)
        {
            selectedIndex = slotIndex;
            _lockSlotIndex = true;
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            combineConsumable.RemoveMaterialsAll();
            enhanceEquipment.RemoveMaterialsAll();
            speechBubbleForEquipment.gameObject.SetActive(false);
            speechBubbleForUpgrade.gameObject.SetActive(false);

            _npc01 = null;

            if (_npc02)
            {
                _npc02.gameObject.SetActive(false);
            }

            _lockSlotIndex = false;

            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            categoryTabArea.SetActive(false);
            equipmentRecipe.gameObject.SetActive(false);
            base.OnCompleteOfCloseAnimationInternal();
        }

        #endregion

        public void OnRecipeCellViewStarClick(LegacyRecipeCellView recipeCellView)
        {
            Debug.LogWarning($"Recipe Star Clicked. {recipeCellView.Model.Row.Id}");
            // 즐겨찾기 등록.

            // 레시피 재정렬.
        }

        public void OnRecipeCellViewSubmitClick(LegacyRecipeCellView recipeCellView)
        {
            if (recipeCellView is null ||
                State.Value != StateType.LegacyCombineConsumable)
                return;

            Debug.LogWarning($"Recipe Submit Clicked. {recipeCellView.Model.Row.Id}");

            var inventoryItemViewModels = new List<InventoryItem>();
            if (recipeCellView.Model.MaterialInfos
                .Any(e =>
                {
                    if (!inventory.SharedModel.TryGetMaterial(e.Id, out var viewModel))
                        return true;

                    inventoryItemViewModels.Add(viewModel);
                    return false;
                }))
                return;

            recipe.Hide();

            combineConsumable.RemoveMaterialsAll();
            combineConsumable.ResetCount();
            foreach (var inventoryItemViewModel in inventoryItemViewModels)
            {
                combineConsumable.TryAddMaterial(inventoryItemViewModel);
            }

            combineConsumable.submitButton.gameObject.SetActive(true);
            ShowBlockIndex(recipeCellView.Model.Row.RequiredBlockIndex);
        }

        private void CheckLockOfCategoryButtons()
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

            var worldInformation = States.Instance.CurrentAvatarState.worldInformation;
            if (!worldInformation.TryGetLastClearedStageId(out var stageId))
            {
                selectionArea.combineEquipmentButton.SetLockVariable(0);
                selectionArea.combineConsumableButton.SetLockVariable(0);
                selectionArea.enhanceEquipmentButton.SetLockVariable(0);

                combineEquipmentCategoryButton.SetLockVariable(0);
                combineConsumableCategoryButton.SetLockVariable(0);
                enhanceEquipmentCategoryButton.SetLockVariable(0);

                return;
            }

            selectionArea.combineEquipmentButton.SetLockVariable(stageId);
            selectionArea.combineConsumableButton.SetLockVariable(stageId);
            selectionArea.enhanceEquipmentButton.SetLockVariable(stageId);

            combineEquipmentCategoryButton.SetLockVariable(stageId);
            combineConsumableCategoryButton.SetLockVariable(stageId);
            enhanceEquipmentCategoryButton.SetLockVariable(stageId);
        }

        private void SubscribeState(StateType value)
        {
            Find<ItemInformationTooltip>().Close();
            recipe.Hide();

            selectionArea.root.SetActive(value == StateType.SelectMenu);
            leftArea.SetActive(value != StateType.SelectMenu);

            switch (value)
            {
                case StateType.SelectMenu:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOffAll();

                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();

                    categoryTabArea.SetActive(false);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(false);
                    consumableRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombineEquipment:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);

                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(true);
                    consumableRecipe.gameObject.SetActive(false);
                    equipmentRecipe.ShowCellViews();
                    recipeAnimator.Play("Show");
                    break;
                case StateType.CombineConsumable:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);

                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(false);
                    consumableRecipe.gameObject.SetActive(true);
                    consumableRecipe.ShowCellViews();
                    recipeAnimator.Play("Show");
                    break;
                case StateType.LegacyCombineConsumable:
                    _selectedSpeechBubble = speechBubbleForUpgrade;
                    speechBubbleForEquipment.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineConsumable.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineConsumable.Contains;

                    combineConsumable.Show(true);
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(true);
                    equipmentRecipe.gameObject.SetActive(false);
                    consumableRecipe.gameObject.SetActive(false);
                    break;
                case StateType.EnhanceEquipment:
                    _selectedSpeechBubble = speechBubbleForUpgrade;
                    speechBubbleForEquipment.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(enhanceEquipmentCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = enhanceEquipment.Contains;

                    combineConsumable.Hide();
                    enhanceEquipment.Show(true);
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_ENHANCE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(true);
                    equipmentRecipe.gameObject.SetActive(false);
                    consumableRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombinationConfirm:
                    _toggleGroup.SetToggledOffAll();

                    var rectTransform = selectedRecipe.transform as RectTransform;
                    recipeClickVFX.transform.position = rectTransform
                        .TransformPoint(rectTransform.rect.center);

                    if (selectedRecipe.ItemSubType == ItemSubType.Food)
                    {
                        recipeClickVFX.OnFinished = OnClickConsumableRecipe;
                    }
                    else
                    {
                        var isElemental = selectedRecipe.ElementalType != ElementalType.Normal;
                        recipeClickVFX.OnFinished = () => OnClickEquipmentRecipe(isElemental);
                    }
                    recipeClickVFX.Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private void OnClickConsumableRecipe()
        {
            _toggleGroup.SetToggledOffAll();

            combineConsumable.Hide();
            enhanceEquipment.Hide();
            equipmentCombinationPanel.Hide();
            ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

            inventory.gameObject.SetActive(false);
            recipeAnimator.Play("Close");
            consumableRecipe.HideCellviews();

            var recipeCellView = selectedRecipe as ConsumableRecipeCellView;
            consumableCombinationPanel.TweenCellView(recipeCellView);
            consumableCombinationPanel.SetData(recipeCellView.RowData);
        }

        private void OnClickEquipmentRecipe(bool isElemental)
        {
            _toggleGroup.SetToggledOffAll();

            combineConsumable.Hide();
            enhanceEquipment.Hide();
            consumableCombinationPanel.Hide();
            ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

            inventory.gameObject.SetActive(false);
            recipeAnimator.Play("Close");
            equipmentRecipe.HideCellviews();

            var recipeCellView = selectedRecipe as EquipmentRecipeCellView;

            if (isElemental)
            {
                equipmentCombinationPanel.Hide();
                elementalCombinationPanel.TweenCellViewInOption(recipeCellView);
                elementalCombinationPanel.SetData(recipeCellView.RowData);
            }
            else
            {
                equipmentCombinationPanel.TweenCellView(recipeCellView);
                equipmentCombinationPanel.SetData(recipeCellView.RowData);
                elementalCombinationPanel.Hide();
            }
        }

        private void ShowTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                LocalizationManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                _ => StageMaterial(view),
                _ => inventory.SharedModel.DeselectItemView());
        }

        private void StageMaterial(InventoryItemView itemView)
        {
            ShowSpeech("SPEECH_COMBINE_STAGE_MATERIAL_");
            switch (State.Value)
            {
                case StateType.LegacyCombineConsumable:
                    combineConsumable.TryAddMaterial(itemView);
                    break;
                case StateType.EnhanceEquipment:
                    enhanceEquipment.TryAddMaterial(itemView);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubscribeOnMaterialChange(LegacyCombinationPanel<CombinationMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void SubscribeOnMaterialChange(LegacyCombinationPanel<EnhancementMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void SubscribeOnToggledOn(IToggleable toggleable)
        {
            if (toggleable.Name.Equals(combineConsumableCategoryButton.Name))
            {
                State.Value = StateType.CombineConsumable;
            }
            else if (toggleable.Name.Equals(combineEquipmentCategoryButton.Name))
            {
                State.Value = StateType.CombineEquipment;
            }
            else if (toggleable.Name.Equals(enhanceEquipmentCategoryButton.Name))
            {
                State.Value = StateType.EnhanceEquipment;
            }
        }

        public void ChangeState(int index)
        {
            State.SetValueAndForceNotify((StateType) index);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            if (State.Value == StateType.SelectMenu)
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            }
            else if (State.Value == StateType.CombinationConfirm)
            {
                if (selectedRecipe.ItemSubType == ItemSubType.Food)
                    State.SetValueAndForceNotify(StateType.CombineConsumable);
                else
                    State.SetValueAndForceNotify(StateType.CombineEquipment);
            }
            else
            {
                State.SetValueAndForceNotify(StateType.SelectMenu);
            }
        }

        private void SubscribeSlotStates(Dictionary<int, CombinationSlotState> states)
        {
            _states = states;
            ResetSelectedIndex();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            ResetSelectedIndex();   
        }

        #region Action

        private void ActionCombineConsumable()
        {
            var rowData = (selectedRecipe as ConsumableRecipeCellView).RowData;

            var materialInfoList = rowData.MaterialItemIds
                .Select(id =>
                {
                    var material = ItemFactory.CreateMaterial(Game.Game.instance.TableSheets.MaterialItemSheet, id);
                    // FIXME : 재료 소모 갯수 대응이 되어있지 않은 상태입니다.
                    return (material, 1);
                }).ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList, selectedIndex);
        }

        private void ActionLegacyCombineConsumable()
        {
            var materialInfoList = combineConsumable.otherMaterials
                .Where(e => !(e is null) && !e.IsEmpty)
                .Select(e => ((Material) e.Model.ItemBase.Value, e.Model.Count.Value))
                .ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList, selectedIndex);
            combineConsumable.RemoveMaterialsAll();
        }

        private void ActionEnhanceEquipment()
        {
            var baseEquipmentGuid =
                ((Equipment) enhanceEquipment.baseMaterial.Model.ItemBase.Value).ItemId;
            var otherEquipmentGuidList = enhanceEquipment.otherMaterials
                .Select(e => ((Equipment) e.Model.ItemBase.Value).ItemId)
                .ToList();

            UpdateCurrentAvatarState(enhanceEquipment, baseEquipmentGuid, otherEquipmentGuidList);
            CreateItemEnhancementAction(baseEquipmentGuid, otherEquipmentGuidList, selectedIndex);
            enhanceEquipment.RemoveMaterialsAll();
        }

        private void ActionEnhancedCombinationEquipment(CombinationPanel combinationPanel)
        {
            var cellview = (combinationPanel.recipeCellView as EquipmentRecipeCellView);
            var model = cellview.RowData;
            var subRecipeId = (combinationPanel is ElementalCombinationPanel elementalPanel)
                ? elementalPanel.SelectedSubRecipeId
                : (int?) null;
            UpdateCurrentAvatarState(combinationPanel, combinationPanel.materialPanel.MaterialList);
            CreateEnhancedCombinationEquipmentAction(
                model.Id,
                subRecipeId,
                selectedIndex,
                model,
                combinationPanel
            );
            equipmentRecipe.UpdateRecipes();
        }

        private static void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
            IEnumerable<(Material material, int count)> materialInfoList)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalStateModifier.ModifyAgentGold(agentAddress, -combinationPanel.CostNCG);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -combinationPanel.CostAP);

            foreach (var (material, count) in materialInfoList)
            {
                LocalStateModifier.RemoveItem(avatarAddress, material.Data.ItemId, count);
            }
        }

        private static void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
            Guid baseItemGuid,
            IEnumerable<Guid> otherItemGuidList)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalStateModifier.ModifyAgentGold(agentAddress, -combinationPanel.CostNCG);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -combinationPanel.CostAP);

            LocalStateModifier.RemoveItem(avatarAddress, baseItemGuid);
            foreach (var itemGuid in otherItemGuidList)
            {
                LocalStateModifier.RemoveItem(avatarAddress, itemGuid);
            }
        }


        private void CreateCombinationAction(List<(Material material, int count)> materialInfoList,
            int slotIndex)
        {
            LocalStateModifier.ModifyCombinationSlotConsumable(
                Game.Game.instance.TableSheets,
                combineConsumable,
                materialInfoList,
                slotIndex
            );
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager.CombinationConsumable(materialInfoList, slotIndex)
                .Subscribe(_ => { },
                    _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
        }

        private void CreateItemEnhancementAction(Guid baseItemGuid,
            IEnumerable<Guid> otherItemGuidList, int slotIndex)
        {
            LocalStateModifier.ModifyCombinationSlotItemEnhancement(
                enhanceEquipment,
                otherItemGuidList,
                slotIndex
            );
            var msg = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager
                .ItemEnhancement(baseItemGuid, otherItemGuidList, slotIndex)
                .Subscribe(_ => { },
                    _ => Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
        }

        private void CreateEnhancedCombinationEquipmentAction(int recipeId, int? subRecipeId,
            int slotIndex, EquipmentItemRecipeSheet.Row model, CombinationPanel panel)
        {
            LocalStateModifier.ModifyCombinationSlot(Game.Game.instance.TableSheets, model, panel,
                slotIndex, subRecipeId);
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager.CombinationEquipment(recipeId, slotIndex, subRecipeId);
        }

        #endregion

        private void ShowSpeech(string key,
            CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (!_npc01)
                return;

            _npc01.PlayAnimation(type == CharacterAnimation.Type.Greeting
                ? NPCAnimation.Type.Greeting_01
                : NPCAnimation.Type.Emotion_01);

            _selectedSpeechBubble.SetKey(key);
            StartCoroutine(_selectedSpeechBubble.CoShowText(true));
        }

        private void ShowBlockIndex(long requiredBlockIndex)
        {
            if (!_npc01)
                return;

            _npc01.PlayAnimation(NPCAnimation.Type.Emotion_01);

            var cost = string.Format(LocalizationManager.Localize("UI_COST_BLOCK"),
                requiredBlockIndex);
            _selectedSpeechBubble.onGoing = true;
            StartCoroutine(_selectedSpeechBubble.CoShowText(cost, true));
        }

        private void ResetSelectedIndex()
        {
            if (!_lockSlotIndex && !(_states is null))
            {
                var pair = _states
                    .FirstOrDefault(i =>
                        i.Value.Validate(
                            States.Instance.CurrentAvatarState,
                            _blockIndex
                        ));
                var idx = pair.Value is null ? -1 : pair.Key;
                selectedIndex = idx;
            }
        }

        private IEnumerator CoCombineNPCAnimation()
        {
            Find<CombinationLoadingScreen>().Show();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Find<BottomMenu>().SetIntractable(false);
            blur.gameObject.SetActive(true);
            _npc01.SpineController.Disappear();
            Push();
            yield return new WaitForSeconds(.5f);
            var go = Game.Game.instance.Stage.npcFactory.Create(NPCId, npcPosition02.position);
            _npc02 = go.GetComponent<NPC>();
            _npc02.SetSortingLayer(LayerType.UI);
            _npc02.SpineController.Appear(.3f);
            _npc02.PlayAnimation(NPCAnimation.Type.Appear_02);
            yield return new WaitForSeconds(5f);
            _npc02.SpineController.Disappear(.3f);
            _npc02.PlayAnimation(NPCAnimation.Type.Disappear_02);
            yield return new WaitForSeconds(.5f);
            _npc02.gameObject.SetActive(false);
            _npc01.SpineController.Appear();
            yield return new WaitForSeconds(1f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Find<BottomMenu>().SetIntractable(true);
            blur.gameObject.SetActive(false);
            Pop();
            _lockSlotIndex = false;
            _selectedSpeechBubble.onGoing = false;
            Find<CombinationLoadingScreen>().Close();
        }
    }
}
