using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
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
using Bencodex.Types;
using Libplanet;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.UI.Model;

namespace Nekoyume.UI
{
    using UniRx;

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
        private ItemRecipe itemRecipe = null;

        [SerializeField]
        private CombinationPanel combinationPanel = null;

        [SerializeField]
        private ElementalCombinationPanel elementalCombinationPanel = null;

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

        [NonSerialized]
        public RecipeCellView selectedRecipe;

        [NonSerialized]
        public int selectedIndex;

        private const string RecipeVFXSkipListKey = "Nekoyume.UI.EquipmentRecipe.FirstEnterRecipeKey_{0}";

        private ToggleGroup _toggleGroup;
        private NPC _npc01;
        private long _blockIndex;
        private Dictionary<int, CombinationSlotState> _states;
        private SpeechBubble _selectedSpeechBubble;
        private int? _equipmentRecipeIdToGo;
        private EnhanceEquipment _enhanceEquipment;

        public Dictionary<int, int[]> RecipeVFXSkipMap { get; private set; }

        public bool HasNotification => itemRecipe.HasNotification;

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
            _toggleGroup.DisabledFunc = () => !CanHandleInputEvent;

            combineEquipmentCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .CombinationEquipmentAction);
            combineConsumableCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .CombinationConsumableAction);
            enhanceEquipmentCategoryButton.SetLockCondition(GameConfig.RequireClearedStageLevel
                .ItemEnhancementAction);

            State.Subscribe(SubscribeState).AddTo(gameObject);

            itemRecipe.Initialize();

            combinationPanel.submitButton.OnSubmitClick.Subscribe(_ => OnCombinationSubmit(combinationPanel))
                .AddTo(gameObject);

            combinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

            elementalCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionCombinationEquipment(elementalCombinationPanel);
                var itemBase = elementalCombinationPanel.recipeCellView.ItemView.Model.ItemBase.Value;
                StartCoroutine(CoCombineNPCAnimation(itemBase, elementalCombinationPanel.SubscribeOnClickSubmit));
            }).AddTo(gameObject);

            elementalCombinationPanel.RequiredBlockIndexSubject.ObserveOnMainThread()
                .Subscribe(ShowBlockIndex).AddTo(gameObject);

            blur.gameObject.SetActive(false);

            CombinationSlotStateSubject.CombinationSlotState.Subscribe(_ => ResetSelectedIndex())
                .AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            CheckLockOfCategoryButtons();

            var hasNotification = HasNotification;
            selectionArea.combineEquipmentButton.HasNotification.Value = hasNotification;
            combineEquipmentCategoryButton.HasNotification.Value = hasNotification;

            Find<CombinationLoadingScreen>().OnDisappear = OnNPCDisappear;

            var stage = Game.Game.instance.Stage;
            stage.LoadBackground("combination");

            var player = stage.GetPlayer();
            player.gameObject.SetActive(false);

            if (_equipmentRecipeIdToGo.HasValue)
            {
                var recipeId = _equipmentRecipeIdToGo.Value;
                var itemId = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values
                    .First(r => r.Id == recipeId).ResultEquipmentId;
                var itemRow = Game.Game.instance.TableSheets.EquipmentItemSheet.Values
                    .First(r => r.Id == itemId);
                itemRecipe.SetToggledOnItemType(itemRow.ItemSubType);
                if (itemRecipe.TryGetCellView(
                    recipeId,
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
                    Debug.LogError($"Not found cell view with {recipeId} in {nameof(itemRecipe)}");
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
                BottomMenu.ToggleableType.Ranking,
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
        }

        public void Show(int slotIndex)
        {
            selectedIndex = slotIndex;
            Show();
        }

        public void ShowByEquipmentRecipe(int recipeId)
        {
            _equipmentRecipeIdToGo = recipeId;
            ResetSelectedIndex();
            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            _enhanceEquipment.Close(ignoreCloseAnimation);
            speechBubbleForEquipment.gameObject.SetActive(false);
            speechBubbleForUpgrade.gameObject.SetActive(false);

            _npc01 = null;

            _equipmentRecipeIdToGo = null;

            base.Close(ignoreCloseAnimation);
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            if (State.Value == StateType.CombinationConfirm)
            {
                return;
            }
            if (State.Value != StateType.SelectMenu)
            {
                State.SetValueAndForceNotify(StateType.SelectMenu);
            }

            categoryTabArea.SetActive(false);
            itemRecipe.gameObject.SetActive(false);
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
            if (_enhanceEquipment is null)
            {
                _enhanceEquipment = Find<EnhanceEquipment>();
            }

            Find<ItemInformationTooltip>().Close();
            Find<BottomMenu>().ToggleGroup.SetToggledOffAll();

            selectionArea.root.SetActive(value == StateType.SelectMenu);
            leftArea.SetActive(value != StateType.SelectMenu);

            switch (value)
            {
                case StateType.SelectMenu:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOffAll();

                    _enhanceEquipment.Hide();
                    combinationPanel.Hide();
                    elementalCombinationPanel.Hide();

                    categoryTabArea.SetActive(false);
                    itemRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombineEquipment:
                    Mixpanel.Track("Unity/Combine Equipment");

                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);

                    _enhanceEquipment.Hide();
                    combinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    if (!categoryTabArea.activeSelf)
                    {
                        Animator.Play("ShowLeftArea", -1, 0.0f);
                        OnTweenRecipe();
                    }

                    categoryTabArea.SetActive(true);
                    itemRecipe.gameObject.SetActive(true);
                    itemRecipe.ShowEquipmentCellViews(_equipmentRecipeIdToGo);
                    itemRecipe.SetState(ItemRecipe.State.Equipment);
                    _equipmentRecipeIdToGo = null;
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);
                    break;
                case StateType.CombineConsumable:
                    _selectedSpeechBubble = speechBubbleForEquipment;
                    speechBubbleForUpgrade.gameObject.SetActive(false);

                    _enhanceEquipment.Hide();
                    combinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

                    if (!categoryTabArea.activeSelf)
                    {
                        Animator.Play("ShowLeftArea", -1, 0.0f);
                        OnTweenRecipe();
                    }
                    categoryTabArea.SetActive(true);
                    itemRecipe.gameObject.SetActive(true);
                    itemRecipe.ShowConsumableCellViews();
                    itemRecipe.SetState(ItemRecipe.State.Consumable);
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);
                    break;
                case StateType.EnhanceEquipment:
                    _selectedSpeechBubble = speechBubbleForUpgrade;
                    speechBubbleForEquipment.gameObject.SetActive(false);
                    _toggleGroup.SetToggledOn(enhanceEquipmentCategoryButton);

                    _enhanceEquipment.Show(true);
                    combinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_ENHANCE_EQUIPMENT_");

                    if (!categoryTabArea.activeSelf)
                    {
                        Animator.Play("ShowLeftArea", -1, 0.0f);
                        OnTweenRecipe();
                    }
                    categoryTabArea.SetActive(true);
                    itemRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombinationConfirm:
                    _toggleGroup.SetToggledOffAll();
                    OnTweenRecipe();

                    if (_equipmentRecipeIdToGo.HasValue)
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

                        _equipmentRecipeIdToGo = null;
                        break;
                    }

                    var rectTransform = (RectTransform) selectedRecipe.transform;
                    var pos = rectTransform.GetWorldPositionOfCenter();
                    var recipeClickVFX = VFXController.instance.CreateAndChaseCam<RecipeClickVFX>(pos);

                    if (selectedRecipe.ItemSubType == ItemSubType.Food)
                    {
                        recipeClickVFX.OnFinished = () =>
                        {
                            OnClickConsumableRecipe();
                            categoryTabArea.SetActive(false);
                            itemRecipe.gameObject.SetActive(false);
                        };
                    }
                    else
                    {
                        var isElemental = selectedRecipe.ElementalType != ElementalType.Normal;
                        recipeClickVFX.OnFinished = () =>
                        {
                            OnClickEquipmentRecipe(isElemental);
                            categoryTabArea.SetActive(false);
                            itemRecipe.gameObject.SetActive(false);
                        };
                    }

                    recipeClickVFX.Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public void UpdateRecipe()
        {
            combineEquipmentCategoryButton.HasNotification.Value = HasNotification;
        }

        private void OnClickRecipe()
        {
            _toggleGroup.SetToggledOffAll();

            _enhanceEquipment.Hide();
            Animator.Play("CloseLeftArea");
        }

        private void OnClickConsumableRecipe()
        {
            OnClickRecipe();

            itemRecipe.HideCellViews();
            ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

            combinationPanel.TweenCellView(selectedRecipe, OnTweenRecipeCompleted);
            combinationPanel.SetData(selectedRecipe.ConsumableRowData);
        }

        private void OnClickEquipmentRecipe(bool isElemental)
        {
            OnClickRecipe();

            ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");
            itemRecipe.HideCellViews();

            if (isElemental)
            {
                combinationPanel.Hide();
                elementalCombinationPanel.TweenCellViewInOption(
                    selectedRecipe,
                    OnTweenRecipeCompleted);
                elementalCombinationPanel.SetData(selectedRecipe.EquipmentRowData);
            }
            else
            {
                combinationPanel.TweenCellView(selectedRecipe, OnTweenRecipeCompleted);
                combinationPanel.SetData(selectedRecipe.EquipmentRowData);
                elementalCombinationPanel.Hide();
            }
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
                    Animator.Play("CloseLeftArea");
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

        private void SubscribeSlotStates(Dictionary<Address, CombinationSlotState> states)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            _states = states
                .ToDictionary(
                    pair => avatarState.combinationSlotAddresses.IndexOf(pair.Key),
                    pair => pair.Value);
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
            var rowData = selectedRecipe.ConsumableRowData;

            var materialInfoList = rowData.Materials
                .Select(info =>
                {
                    var material = ItemFactory.CreateMaterial(
                        Game.Game.instance.TableSheets.MaterialItemSheet,
                        info.Id);
                    return (material, info.Count);
                }).ToList();

            UpdateCurrentAvatarState(combinationPanel, materialInfoList);
            CreateConsumableCombinationAction(rowData, selectedIndex);
        }

        private void ActionCombinationEquipment(CombinationPanel combinationPanel)
        {
            var cellview = combinationPanel.recipeCellView;
            var model = cellview.EquipmentRowData;
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
            combineEquipmentCategoryButton.HasNotification.Value = HasNotification;
        }

        private static void UpdateCurrentAvatarState(
            ICombinationPanel combinationPanel,
            IEnumerable<(Material material, int count)> materialInfoList)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -combinationPanel.CostNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -combinationPanel.CostAP);

            foreach (var (material, count) in materialInfoList)
            {
                LocalLayerModifier.RemoveItem(avatarAddress, material.ItemId, count);
            }
        }

        private void CreateConsumableCombinationAction(ConsumableItemRecipeSheet.Row row, int slotIndex)
        {
            LocalLayerModifier.ModifyCombinationSlotConsumable(
                Game.Game.instance.TableSheets,
                combinationPanel,
                row,
                slotIndex
            );
            Game.Game.instance.ActionManager.CombinationConsumable(row.Id, slotIndex)
                .Subscribe(
                    _ => { },
                    e => ActionRenderHandler.BackToMain(false, e));
        }

        private void CreateCombinationEquipmentAction(
            int recipeId,
            int? subRecipeId,
            int slotIndex,
            EquipmentItemRecipeSheet.Row model,
            CombinationPanel panel)
        {
            LocalLayerModifier.ModifyCombinationSlotEquipment(
                Game.Game.instance.TableSheets,
                model,
                panel,
                slotIndex,
                subRecipeId);
            Game.Game.instance.ActionManager.CombinationEquipment(recipeId, slotIndex, subRecipeId);
        }

        #endregion

        public void ShowSpeech(string key,
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

            var cost = string.Format(L10nManager.Localize("UI_COST_BLOCK"),
                requiredBlockIndex);
            StartCoroutine(_selectedSpeechBubble.CoShowText(cost, true, true));
        }

        private void ResetSelectedIndex()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var slotStates = States.Instance.CombinationSlotStates;
            if (avatarState is null || slotStates is null)
            {
                return;
            }
            var avatarAddress = avatarState.address;
            var idx = -1;
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                var address = avatarAddress.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );

                if (slotStates.ContainsKey(address))
                {
                    var state = slotStates[address];
                    if (state.Validate(avatarState, _blockIndex))
                    {
                        idx = i;
                        break;
                    }
                }
            }

            selectedIndex = idx;
            if (selectedIndex < 0)
            {
                Debug.Log("There is no valid slot in combination slot state.");
            }

            _enhanceEquipment.UpdateSubmittable();
            combinationPanel.UpdateSubmittable();
            elementalCombinationPanel.UpdateSubmittable();
        }

        public IEnumerator CoCombineNPCAnimation(ItemBase itemBase, System.Action action, bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(action);
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
            _selectedSpeechBubble.Hide();
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
            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
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
                RecipeVFXSkipMap[recipe.Id] = recipe.SubRecipeIds.Take(3).ToArray();
            }

            SaveRecipeVFXSkipMap();
        }

        public void SaveRecipeVFXSkipMap()
        {
            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
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

        private void OnCombinationSubmit(CombinationPanel panel)
        {
            ItemBase itemBase;
            var isConsumable = false;
            switch (panel.stateType)
            {
                case StateType.CombineEquipment:
                    Mixpanel.Track("Unity/Craft Sword");

                    ActionCombinationEquipment(combinationPanel);
                    itemBase = panel.recipeCellView.ItemView.Model.ItemBase.Value;
                    break;
                case StateType.CombineConsumable:
                    ActionCombineConsumable();
                    var rowData =
                        Game.Game.instance.TableSheets.ConsumableItemSheet.Values.FirstOrDefault(r =>
                            r.Id == selectedRecipe.ConsumableRowData
                                .ResultConsumableItemId);
                    itemBase = new Consumable(rowData, Guid.Empty, 0);
                    isConsumable = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(panel.stateType), panel.stateType, null);
            }
            StartCoroutine(CoCombineNPCAnimation(itemBase, panel.SubscribeOnClickSubmit, isConsumable));
        }

        public void TutorialActionClickFirstRecipeCellView() =>
            itemRecipe.OnClickCellViewFromTutorial();

        public void TutorialActionClickCombinationSubmitButton() =>
            OnCombinationSubmit(combinationPanel);

        public void TutorialActionCloseCombination()
        {
            Close();

            if (gameObject.activeSelf)
            {
                Game.Event.OnRoomEnter.Invoke(true);
            }
        }
    }
}
