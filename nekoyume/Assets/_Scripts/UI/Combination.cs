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
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI
{
    public class Combination : Widget, RecipeCellView.IEventListener
    {
        public enum StateType
        {
            SelectMenu,
            NewCombineEquipment,
            CombineConsumable,
            EnhanceEquipment,
            CombineEquipment,
            CombinationConfirm,
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.SelectMenu);

        private const int NPCId = 300001;

        private ToggleGroup _toggleGroup;
        public CategoryButton combineEquipmentCategoryButton;
        public CategoryButton combineConsumableCategoryButton;
        public CategoryButton enhanceEquipmentCategoryButton;

        public GameObject leftArea;
        public GameObject categoryTabArea;
        public GameObject selectionArea;
        public EquipmentRecipe equipmentRecipe;

        public Module.Inventory inventory;

        public CombineEquipment combineEquipment;
        public CombineConsumable combineConsumable;
        public EnhanceEquipment enhanceEquipment;
        public EquipmentCombinationPanel equipmentCombinationPanel;
        public ElementalCombinationPanel elementalCombinationPanel;
        public Recipe recipe;
        public SpeechBubble speechBubble;
        public Transform npcPosition01;
        public Transform npcPosition02;
        public CanvasGroup canvasGroup;
        public ModuleBlur blur;

        private NPC _npc01;
        private NPC _npc02;


        #region Override

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () => {};
        }

        public override void Initialize()
        {
            base.Initialize();

            _toggleGroup = new ToggleGroup();
            _toggleGroup.OnToggledOn.Subscribe(SubscribeOnToggledOn).AddTo(gameObject);
            _toggleGroup.RegisterToggleable(combineEquipmentCategoryButton);
            _toggleGroup.RegisterToggleable(combineConsumableCategoryButton);
            _toggleGroup.RegisterToggleable(enhanceEquipmentCategoryButton);

            State.Subscribe(SubscribeState).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView.Subscribe(StageMaterial).AddTo(gameObject);

            combineEquipment.RemoveMaterialsAll();
            combineEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineEquipment.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionCombineEquipment();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);
            
            combineConsumable.RemoveMaterialsAll();
            combineConsumable.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineConsumable.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionCombineConsumable();
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);
            combineConsumable.recipeButton.OnClickAsObservable().Subscribe(_ =>
            {
                combineConsumable.submitButton.gameObject.SetActive(false);
                recipe.Show();
            }).AddTo(gameObject);

            enhanceEquipment.RemoveMaterialsAll();
            enhanceEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
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

            elementalCombinationPanel.submitButton.OnSubmitClick.Subscribe(_ =>
            {
                ActionEnhancedCombinationEquipment(elementalCombinationPanel);
                StartCoroutine(CoCombineNPCAnimation());
            }).AddTo(gameObject);

            recipe.RegisterListener(this);
            recipe.closeButton.OnClickAsObservable()
                .Subscribe(_ => combineConsumable.submitButton.gameObject.SetActive(true)).AddTo(gameObject);

            blur.gameObject.SetActive(false);
        }

        private IEnumerator CoCombineNPCAnimation()
        {
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
        }

        public override void Show()
        {
            base.Show();

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
                BottomMenu.ToggleableType.Inventory);

            var go = Game.Game.instance.Stage.npcFactory.Create(NPCId, npcPosition01.position);
            _npc01 = go.GetComponent<NPC>();

            ShowSpeech("SPEECH_COMBINE_GREETING_", CharacterAnimation.Type.Greeting);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            combineEquipment.RemoveMaterialsAll();
            combineConsumable.RemoveMaterialsAll();
            enhanceEquipment.RemoveMaterialsAll();
            speechBubble.gameObject.SetActive(false);
            
            if (_npc01)
            {
                _npc01.gameObject.SetActive(false);
            }

            if (_npc02)
            {
                _npc02.gameObject.SetActive(false);
            }

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void OnRecipeCellViewStarClick(RecipeCellView recipeCellView)
        {
            Debug.LogWarning($"Recipe Star Clicked. {recipeCellView.Model.Row.Id}");
            // 즐겨찾기 등록.

            // 레시피 재정렬.
        }

        public void OnRecipeCellViewSubmitClick(RecipeCellView recipeCellView)
        {
            if (recipeCellView is null ||
                State.Value != StateType.CombineConsumable)
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
        }

        private void SubscribeState(StateType value)
        {
            inventory.Tooltip.Close();
            recipe.Hide();

            selectionArea.SetActive(value == StateType.SelectMenu);
            leftArea.SetActive(value != StateType.SelectMenu);

            switch (value)
            {
                case StateType.SelectMenu:
                    _toggleGroup.SetToggledOffAll();

                    combineEquipment.Hide();
                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();

                    categoryTabArea.SetActive(false);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(false);
                    break;
                case StateType.NewCombineEquipment:
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);

                    combineEquipment.Hide();
                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(true);
                    break;
                case StateType.CombineEquipment:
                    _toggleGroup.SetToggledOn(combineEquipmentCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineEquipment.Contains;

                    combineEquipment.Show(true);
                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(true);
                    equipmentRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombineConsumable:
                    _toggleGroup.SetToggledOn(combineConsumableCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineConsumable.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineConsumable.Contains;

                    combineEquipment.Hide();
                    combineConsumable.Show(true);
                    enhanceEquipment.Hide();
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(true);
                    equipmentRecipe.gameObject.SetActive(false);
                    break;  
                case StateType.EnhanceEquipment:
                    _toggleGroup.SetToggledOn(enhanceEquipmentCategoryButton);

                    inventory.SharedModel.DeselectItemView();
                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = enhanceEquipment.Contains;

                    combineEquipment.Hide();
                    combineConsumable.Hide();
                    enhanceEquipment.Show(true);
                    equipmentCombinationPanel.Hide();
                    elementalCombinationPanel.Hide();
                    ShowSpeech("SPEECH_COMBINE_ENHANCE_EQUIPMENT_");

                    categoryTabArea.SetActive(true);
                    inventory.gameObject.SetActive(true);
                    equipmentRecipe.gameObject.SetActive(false);
                    break;
                case StateType.CombinationConfirm:
                    _toggleGroup.SetToggledOffAll();

                    combineEquipment.Hide();
                    combineConsumable.Hide();
                    enhanceEquipment.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");

                    categoryTabArea.SetActive(false);
                    inventory.gameObject.SetActive(false);
                    equipmentRecipe.gameObject.SetActive(false);

                    var selectedRecipe = equipmentRecipe.selectedRecipe;
                    var isElemental = selectedRecipe.elementalType != ElementalType.Normal;

                    if (isElemental)
                    {
                        // 여기서 옵션 선택 화면을 보여준다.
                        elementalCombinationPanel.SetData(selectedRecipe);
                        equipmentCombinationPanel.Hide();
                    }
                    else
                    {
                        equipmentCombinationPanel.SetData(selectedRecipe);
                        elementalCombinationPanel.Hide();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public void ShowLegacyCombineEquipment()
        {
            State.Value = StateType.CombineEquipment;
        }

        private void ShowTooltip(InventoryItemView view)
        {
            if (view is null ||
                view.RectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();
                return;
            }

            inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                LocalizationManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                tooltip => StageMaterial(view),
                tooltip => inventory.SharedModel.DeselectItemView());
        }

        private void StageMaterial(InventoryItemView itemView)
        {
            ShowSpeech("SPEECH_COMBINE_STAGE_MATERIAL_");
            switch (State.Value)
            {
                case StateType.CombineConsumable:
                    combineConsumable.TryAddMaterial(itemView);
                    break;
                case StateType.CombineEquipment:
                    combineEquipment.TryAddMaterial(itemView);
                    break;
                case StateType.EnhanceEquipment:
                    enhanceEquipment.TryAddMaterial(itemView);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SubscribeOnMaterialChange(CombinationPanel<CombinationMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void SubscribeOnMaterialChange(CombinationPanel<EnhancementMaterialView> viewModel)
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
                State.Value = StateType.NewCombineEquipment;
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
            if (State.Value == StateType.SelectMenu)
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
            }
            else if (State.Value == StateType.CombinationConfirm)
            {
                State.SetValueAndForceNotify(StateType.NewCombineEquipment);
            }
            else
            {
                State.SetValueAndForceNotify(StateType.SelectMenu);
            }
        }

        #region Action

        private void ActionCombineConsumable()
        {
            var materialInfoList = combineConsumable.otherMaterials
                .Where(e => !(e is null) && !e.IsEmpty)
                .Select(e => ((Material) e.Model.ItemBase.Value, e.Model.Count.Value))
                .ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList);
            combineConsumable.RemoveMaterialsAll();
        }

        private void ActionCombineEquipment()
        {
            var materialInfoList = new List<(Material material, int value)>();
            materialInfoList.Add((
                (Material) combineEquipment.baseMaterial.Model.ItemBase.Value,
                combineEquipment.baseMaterial.Model.Count.Value));
            materialInfoList.AddRange(combineEquipment.otherMaterials
                .Where(e => !(e is null) && !(e.Model is null))
                .Select(e => ((Material)e.Model.ItemBase.Value, e.Model.Count.Value)));

            UpdateCurrentAvatarState(combineEquipment, materialInfoList);
            CreateCombinationAction(materialInfoList);
            combineEquipment.RemoveMaterialsAll();
        }

        private void ActionEnhanceEquipment()
        {
            var baseEquipmentGuid = ((Equipment) enhanceEquipment.baseMaterial.Model.ItemBase.Value).ItemId;
            var otherEquipmentGuidList = enhanceEquipment.otherMaterials
                .Select(e => ((Equipment) e.Model.ItemBase.Value).ItemId)
                .ToList();

            UpdateCurrentAvatarState(enhanceEquipment, baseEquipmentGuid, otherEquipmentGuidList);
            CreateItemEnhancementAction(baseEquipmentGuid, otherEquipmentGuidList);
            enhanceEquipment.RemoveMaterialsAll();
        }

        private void ActionEnhancedCombinationEquipment(EquipmentCombinationPanel combinationPanel)
        {
            var model = combinationPanel.recipeCellView.model;
            var subRecipeId = (combinationPanel is ElementalCombinationPanel elementalPanel) ?
                elementalPanel.SelectedSubRecipeId
                : (int?) null;
            UpdateCurrentAvatarState(combinationPanel, combinationPanel.materialPanel.MaterialList);
            CreateEnhancedCombinationEquipmentAction(model.Id, subRecipeId);
        }

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
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

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel, Guid baseItemGuid,
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


        private void CreateCombinationAction(List<(Material material, int count)> materialInfoList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager.Combination(materialInfoList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
        }

        private void CreateItemEnhancementAction(Guid baseItemGuid, IEnumerable<Guid> otherItemGuidList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager.ItemEnhancement(baseItemGuid, otherItemGuidList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
        }

        private void CreateEnhancedCombinationEquipmentAction(int recipeId, int? subRecipeId)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager.CombinationEquipment(recipeId, subRecipeId);
        }

        #endregion

        private void ShowSpeech(string key, CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (!_npc01)
                return;

            _npc01.PlayAnimation(type == CharacterAnimation.Type.Greeting
                ? NPCAnimation.Type.Greeting_01
                : NPCAnimation.Type.Emotion_01);
            
            speechBubble.SetKey(key);
            StartCoroutine(speechBubble.CoShowText());
        }
    }
}
