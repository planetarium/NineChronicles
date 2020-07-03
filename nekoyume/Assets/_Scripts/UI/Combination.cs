using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
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
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;
using Nekoyume.Game.VFX;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using mixpanel;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        public enum StateType
        {
            SelectMenu,
            CombineEquipment,
            CombineConsumable,
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

        private struct RecipeIdSet
        {
            public int recipeId;
            public int? subRecipeId;
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.SelectMenu);

        private const int NPCId = 300001;

        [SerializeField]
        private SelectionArea selectionArea = default;

        [SerializeField]
        private CategoryButton combineEquipmentCategoryButton = null;

        [SerializeField]
        private CategoryButton combineConsumableCategoryButton = null;

        [SerializeField]
        private CategoryButton enhanceEquipmentCategoryButton = null;

        [SerializeField]
        private GameObject leftArea = null;

        [SerializeField]
        private GameObject categoryTabArea = null;

        [SerializeField]
        private EquipmentRecipe equipmentRecipe = null;

        [SerializeField]
        private ConsumableRecipe consumableRecipe = null;

        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private EnhanceEquipment enhanceEquipment = null;

        [SerializeField]
        private EquipmentCombinationPanel equipmentCombinationPanel = null;

        [SerializeField]
        private ElementalCombinationPanel elementalCombinationPanel = null;

        [SerializeField]
        private ConsumableCombinationPanel consumableCombinationPanel = null;

        [SerializeField]
        private SpeechBubble speechBubbleForEquipment = null;

        [SerializeField]
        private SpeechBubble speechBubbleForUpgrade = null;

        [SerializeField]
        private Transform npcPosition01 = null;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private ModuleBlur blur = null;

        [SerializeField]
        private RecipeClickVFX recipeClickVFX = null;

        [NonSerialized]
        public RecipeCellView selectedRecipe;

        [NonSerialized]
        public int selectedIndex;

        private const string RecipeVFXSkipListKey = "Nekoyume.UI.EquipmentRecipe.FirstEnterRecipeKey_{0}";

        private ToggleGroup _toggleGroup;
        private NPC _npc01;
        private bool _lockSlotIndex;
        private long _blockIndex;
        private Dictionary<int, CombinationSlotState> _states;
        private SpeechBubble _selectedSpeechBubble;
        private RecipeIdSet? _shouldGoToEquipmentRecipe;

        public Dictionary<int, int[]> RecipeVFXSkipMap { get; private set; }

        public bool HasNotification => equipmentRecipe.HasNotification();

        public override bool CanHandleInputEvent => State.Value == StateType.CombinationConfirm
            ? AnimationState == AnimationStateType.Shown
            : base.CanHandleInputEvent;

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

            equipmentRecipe.Initialize();
            consumableRecipe.Initialize();

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
                Mixpanel.Track("Unity/Craft Sword");
                if (State.Value == StateType.CombinationConfirm)
                    return;

                ActionCombinationEquipment(equipmentCombinationPanel);
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            equipmentCombinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

            elementalCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                if (State.Value == StateType.CombinationConfirm)
                    return;

                ActionCombinationEquipment(elementalCombinationPanel);
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            consumableCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                if (State.Value == StateType.CombinationConfirm)
                    return;

                ActionCombineConsumable();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            elementalCombinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

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

            Find<CombinationLoadingScreen>().OnDisappear = OnNPCDisappear;

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("combination");

            var player = stage.GetPlayer();
            player.gameObject.SetActive(false);

            if (_shouldGoToEquipmentRecipe.HasValue)
            {
                equipmentRecipe.UpdateRecipes();
                if (_shouldGoToEquipmentRecipe.Value.subRecipeId.HasValue)
                {
                    if (equipmentRecipe.TryGetCellView(
                        _shouldGoToEquipmentRecipe.Value.recipeId,
                        out var cellView))
                    {
                        if (cellView.IsLocked)
                        {
                            State.SetValueAndForceNotify(StateType.CombineEquipment);
                        }
                        else
                        {
                            selectedRecipe = cellView;
                            State.SetValueAndForceNotify(StateType.CombinationConfirm);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Not found cell view with {_shouldGoToEquipmentRecipe.Value.recipeId} in {nameof(equipmentRecipe)}");
                        State.SetValueAndForceNotify(StateType.CombineEquipment);
                    }
                }
                else
                {
                    State.SetValueAndForceNotify(StateType.CombineEquipment);
                }
            }
            else
            {
                State.SetValueAndForceNotify(StateType.SelectMenu);
            }

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook,
                BottomMenu.ToggleableType.Character,
                BottomMenu.ToggleableType.Combination
            );

            if (_npc01 is null)
            {
                var go = Game.Game.instance.Stage.npcFactory.Create(
                    NPCId,
                    npcPosition01.position,
                    LayerType.InGameBackground,
                    3);
                _npc01 = go.GetComponent<NPC>();
            }

            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
            Find<CelebratesPopup>().Show(GuidedQuest.CombinationEquipmentQuest);
        }

        public void Show(int slotIndex)
        {
            selectedIndex = slotIndex;
            _lockSlotIndex = true;
            Show();
        }

        public void ShowByEquipmentRecipe(int recipeId, int? subRecipeId)
        {
            _shouldGoToEquipmentRecipe = new RecipeIdSet
            {
                recipeId = recipeId,
                subRecipeId = subRecipeId
            };

            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            enhanceEquipment.RemoveMaterialsAll();
            speechBubbleForEquipment.gameObject.SetActive(false);
            speechBubbleForUpgrade.gameObject.SetActive(false);

            _npc01 = null;

            _lockSlotIndex = false;
            _shouldGoToEquipmentRecipe = null;

            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            if (State.Value == StateType.CombinationConfirm)
            {
                return;
            }

            categoryTabArea.SetActive(false);
            equipmentRecipe.gameObject.SetActive(false);
            base.OnCompleteOfCloseAnimationInternal();
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            ShowSpeech("SPEECH_COMBINE_GREETING_", CharacterAnimation.Type.Greeting);
            HelpPopup.HelpMe(100007);
        }

        #endregion

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

            selectionArea.root.SetActive(value == StateType.SelectMenu);
            leftArea.SetActive(value != StateType.SelectMenu);

            switch (value)
            {
                case StateType.SelectMenu:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOffAll();

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
                    Mixpanel.Track("Unity/Combine Equipment");
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);

                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    consumableCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(true);
                    consumableRecipe.gameObject.SetActive(false);
                    equipmentRecipe.ShowCellViews(_shouldGoToEquipmentRecipe?.recipeId);
                    _shouldGoToEquipmentRecipe = null;
                    Animator.Play("ShowLeftArea", -1, 0.0f);
                    OnTweenRecipe();
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);
                    break;
                case StateType.CombineConsumable:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);

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
                    Animator.Play("ShowLeftArea", -1, 0.0f);
                    OnTweenRecipe();
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);
                    break;
                case StateType.EnhanceEquipment:
                    _selectedSpeechBubble = speechBubbleForUpgrade;
                    speechBubbleForEquipment.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(enhanceEquipmentCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = enhanceEquipment.Contains;

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
                    OnTweenRecipe();

                    if (_shouldGoToEquipmentRecipe.HasValue)
                    {
                        if (selectedRecipe.ItemSubType == ItemSubType.Food)
                        {
                            OnClickConsumableRecipe();
                        }
                        else
                        {
                            var isElemental = selectedRecipe.ElementalType != ElementalType.Normal;
                            OnClickEquipmentRecipe(isElemental);
                        }

                        _shouldGoToEquipmentRecipe = null;
                        break;
                    }

                    var rectTransform = (RectTransform) selectedRecipe.transform;
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

        public void UpdateRecipe()
        {
            equipmentRecipe.UpdateRecipes();
            consumableRecipe.UpdateRecipes();
        }

        private void OnClickRecipe()
        {
            _toggleGroup.SetToggledOffAll();

            enhanceEquipment.Hide();
            inventory.gameObject.SetActive(false);
            Animator.Play("CloseLeftArea");
        }

        private void OnClickConsumableRecipe()
        {
            OnClickRecipe();

            equipmentCombinationPanel.Hide();
            ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");
            consumableRecipe.HideCellViews();

            var recipeCellView = selectedRecipe as ConsumableRecipeCellView;
            consumableCombinationPanel.TweenCellView(recipeCellView, OnTweenRecipeCompleted);
            consumableCombinationPanel.SetData(recipeCellView.RowData);
        }

        private void OnClickEquipmentRecipe(bool isElemental)
        {
            OnClickRecipe();

            consumableCombinationPanel.Hide();
            ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");
            equipmentRecipe.HideCellViews();

            var recipeCellView = selectedRecipe as EquipmentRecipeCellView;

            if (isElemental)
            {
                equipmentCombinationPanel.Hide();
                elementalCombinationPanel.TweenCellViewInOption(
                    recipeCellView,
                    OnTweenRecipeCompleted);
                elementalCombinationPanel.SetData(recipeCellView.RowData);
            }
            else
            {
                equipmentCombinationPanel.TweenCellView(recipeCellView, OnTweenRecipeCompleted);
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
                case StateType.EnhanceEquipment:
                    enhanceEquipment.TryAddMaterial(itemView);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubscribeOnMaterialChange(EnhancementPanel<EnhancementMaterialView> viewModel)
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

            switch (State.Value)
            {
                case StateType.SelectMenu:
                    Close();
                    Game.Event.OnRoomEnter.Invoke(true);
                    break;
                case StateType.CombinationConfirm:
                    State.SetValueAndForceNotify(selectedRecipe.ItemSubType == ItemSubType.Food
                        ? StateType.CombineConsumable
                        : StateType.CombineEquipment);
                    break;
                default:
                    State.SetValueAndForceNotify(StateType.SelectMenu);
                    break;
            }
        }

        public void OnTweenRecipe()
        {
            AnimationState = AnimationStateType.Showing;
        }

        public void OnTweenRecipeCompleted()
        {
            AnimationState = AnimationStateType.Shown;
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
                    var material = ItemFactory.CreateMaterial(
                        Game.Game.instance.TableSheets.MaterialItemSheet,
                        id);
                    // FIXME : 재료 소모 갯수 대응이 되어있지 않은 상태입니다.
                    return (material, 1);
                }).ToList();

            UpdateCurrentAvatarState(consumableCombinationPanel, materialInfoList);
            CreateConsumableCombinationAction(rowData.Id, materialInfoList, selectedIndex);
            consumableRecipe.UpdateRecipes();
        }

        private void ActionCombinationEquipment(CombinationPanel combinationPanel)
        {
            var cellview = (combinationPanel.recipeCellView as EquipmentRecipeCellView);
            var model = cellview.RowData;
            var subRecipeId = (combinationPanel is ElementalCombinationPanel elementalPanel)
                ? elementalPanel.SelectedSubRecipeId
                : (int?) null;
            UpdateCurrentAvatarState(combinationPanel, combinationPanel.materialPanel.MaterialList);
            CreateCombinationEquipmentAction(
                model.Id,
                subRecipeId,
                selectedIndex,
                model,
                combinationPanel
            );
            equipmentRecipe.UpdateRecipes();
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

        private static void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
            IEnumerable<(Material material, int count)> materialInfoList)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalStateModifier.ModifyAgentGold(agentAddress, -combinationPanel.CostNCG);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -combinationPanel.CostAP);

            foreach (var (material, count) in materialInfoList)
            {
                LocalStateModifier.RemoveItem(avatarAddress, material.ItemId, count);
            }
        }

        private static void UpdateCurrentAvatarState(
            ICombinationPanel combinationPanel,
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


        private void CreateConsumableCombinationAction(
            int rowId,
            List<(Material material, int count)> materialInfoList,
            int slotIndex)
        {
            LocalStateModifier.ModifyCombinationSlotConsumable(
                Game.Game.instance.TableSheets,
                consumableCombinationPanel,
                materialInfoList,
                slotIndex
            );
            Game.Game.instance.ActionManager.CombinationConsumable(rowId, slotIndex)
                .Subscribe(
                    _ => { },
                    _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
        }

        private void CreateCombinationEquipmentAction(
            int recipeId,
            int? subRecipeId,
            int slotIndex,
            EquipmentItemRecipeSheet.Row model,
            CombinationPanel panel)
        {
            LocalStateModifier.ModifyCombinationSlot(
                Game.Game.instance.TableSheets,
                model,
                panel,
                slotIndex,
                subRecipeId);
            Game.Game.instance.ActionManager.CombinationEquipment(recipeId, slotIndex, subRecipeId);
        }

        private void CreateItemEnhancementAction(
            Guid baseItemGuid,
            List<Guid> otherItemGuidList,
            int slotIndex)
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
                .Subscribe(
                    _ => { },
                    _ => Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
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
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Find<BottomMenu>().SetIntractable(false);
            blur.gameObject.SetActive(true);
            _npc01.SpineController.Disappear();
            Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.AnimateNPC();
        }

        private void OnNPCDisappear()
        {
            _npc01.SpineController.Appear();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Find<BottomMenu>().SetIntractable(true);
            blur.gameObject.SetActive(false);
            Pop();
            _lockSlotIndex = false;
            _selectedSpeechBubble.onGoing = false;
        }

        private void SetNPCAlphaZero()
        {
            _npc01.SpineController.SkeletonAnimation.skeleton.A = 0;
        }

        private void NPCShowAnimation()
        {
            var skeletonTweener = DOTween.To(
                () => _npc01.SpineController.SkeletonAnimation.skeleton.A,
                alpha => _npc01.SpineController.SkeletonAnimation.skeleton.A = alpha, 1,
                1f);
            skeletonTweener.Play();
        }

        public void LoadRecipeVFXSkipMap()
        {
            var addressHex = ReactiveAvatarState.Address.Value.ToHex();
            var key = string.Format(RecipeVFXSkipListKey, addressHex);

            if (!PlayerPrefs.HasKey(key))
            {
                CreateRecipeVFXSkipMap();
            }
            else
            {
                var bf = new BinaryFormatter();
                var data = PlayerPrefs.GetString(key);
                var bytes = Convert.FromBase64String(data);

                using (var ms = new MemoryStream(bytes))
                {
                    var obj = bf.Deserialize(ms);

                    if (!(obj is Dictionary<int, int[]>))
                    {
                        CreateRecipeVFXSkipMap();
                    }
                    else
                    {
                        RecipeVFXSkipMap = (Dictionary<int, int[]>)obj;
                    }
                }
            }
        }

        public void CreateRecipeVFXSkipMap()
        {
            RecipeVFXSkipMap = new Dictionary<int, int[]>();

            var gameInstance = Game.Game.instance;

            var recipeTable = gameInstance.TableSheets.EquipmentItemRecipeSheet;
            var subRecipeTable = gameInstance.TableSheets.EquipmentItemSubRecipeSheet;
            var worldInfo = gameInstance.States.CurrentAvatarState.worldInformation;

            foreach (var recipe in recipeTable.Values
                .Where(x => worldInfo.IsStageCleared(x.UnlockStage)))
            {
                var unlockedSubRecipes = recipe.SubRecipeIds
                    .Where(id => worldInfo
                    .IsStageCleared(subRecipeTable[id].UnlockStage));

                RecipeVFXSkipMap[recipe.Id] = unlockedSubRecipes.Take(3).ToArray();
            }

            SaveRecipeVFXSkipMap();
        }

        public void SaveRecipeVFXSkipMap()
        {
            var addressHex = ReactiveAvatarState.Address.Value.ToHex();
            var key = string.Format(RecipeVFXSkipListKey, addressHex);

            var data = string.Empty;
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, RecipeVFXSkipMap);
                var bytes = ms.ToArray();
                data = Convert.ToBase64String(bytes);
            }

            PlayerPrefs.SetString(key, data);
        }
    }
}
