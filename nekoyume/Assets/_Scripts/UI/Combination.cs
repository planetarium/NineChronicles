using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.UI
{
    public class Combination : Widget, RecipeCellView.IEventListener
    {
        public enum StateType
        {
            CombineConsumable,
            CombineEquipment,
            EnhanceEquipment
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.CombineEquipment);

        private const int NpcId = 300001;
        private static readonly UnityEngine.Vector3 NpcPosition = new UnityEngine.Vector3(2.28f, -1.72f);

        public CategoryButton combineConsumableCategoryButton;
        public CategoryButton combineEquipmentCategoryButton;
        public CategoryButton enhanceEquipmentCategoryButton;

        public Module.Inventory inventory;

        public CombineConsumable combineConsumable;
        public CombineEquipment combineEquipment;
        public EnhanceEquipment enhanceEquipment;
        public SpeechBubble speechBubble;

        private Npc _npc;

        public Recipe recipe;

        #region Override

        public override void Initialize()
        {
            base.Initialize();

            State.Subscribe(SubscribeState).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.SharedModel.OnDoubleClickItemView.Subscribe(StageMaterial).AddTo(gameObject);

            combineConsumable.RemoveMaterialsAll();
            combineConsumable.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineConsumable.submitButton.OnSubmitClick.Subscribe(_ => ActionCombineConsumable()).AddTo(gameObject);
            combineConsumable.recipeButton.OnClickAsObservable().Subscribe(_ =>
            {
                combineConsumable.submitButton.gameObject.SetActive(false);
                recipe.Show();
            }).AddTo(gameObject);

            combineEquipment.RemoveMaterialsAll();
            combineEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            combineEquipment.submitButton.OnSubmitClick.Subscribe(_ => ActionCombineEquipment()).AddTo(gameObject);

            enhanceEquipment.RemoveMaterialsAll();
            enhanceEquipment.OnMaterialChange.Subscribe(SubscribeOnMaterialChange).AddTo(gameObject);
            enhanceEquipment.submitButton.OnSubmitClick.Subscribe(_ => ActionEnhanceEquipment()).AddTo(gameObject);

            recipe.RegisterListener(this);
            recipe.closeButton.OnClickAsObservable()
                .Subscribe(_ => combineConsumable.submitButton.gameObject.SetActive(true)).AddTo(gameObject);

            combineEquipmentCategoryButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    State.Value = StateType.CombineEquipment;
                })
                .AddTo(gameObject);
            combineConsumableCategoryButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    State.Value = StateType.CombineConsumable;
                })
                .AddTo(gameObject);
            enhanceEquipmentCategoryButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    State.Value = StateType.EnhanceEquipment;
                })
                .AddTo(gameObject);
        }

        public override void Show()
        {
            base.Show();

            var stage = Game.Game.instance.stage;
            stage.LoadBackground("combination");
            var player = stage.GetPlayer();
            player.gameObject.SetActive(false);

            State.SetValueAndForceNotify(StateType.CombineEquipment);

            Find<BottomMenu>().Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick);

            var go = Game.Game.instance.stage.npcFactory.Create(NpcId, NpcPosition);
            _npc = go.GetComponent<Npc>();
            go.SetActive(true);

            ShowSpeech("SPEECH_COMBINE_GREETING_", CharacterAnimation.Type.Greeting);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            combineConsumable.RemoveMaterialsAll();
            combineEquipment.RemoveMaterialsAll();
            enhanceEquipment.RemoveMaterialsAll();

            base.Close(ignoreCloseAnimation);

            _npc.gameObject.SetActive(false);
            speechBubble.gameObject.SetActive(false);
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
        }

        private void SubscribeState(StateType value)
        {
            inventory.Tooltip.Close();
            inventory.SharedModel.DeselectItemView();
            recipe.Hide();

            switch (value)
            {
                case StateType.CombineConsumable:
                    combineConsumableCategoryButton.SetToggledOn();
                    combineEquipmentCategoryButton.SetToggledOff();

                    enhanceEquipmentCategoryButton.SetToggledOff();

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineConsumable.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineConsumable.Contains;

                    combineConsumable.Show(true);
                    combineEquipment.Hide();
                    enhanceEquipment.Hide();
                    ShowSpeech("SPEECH_COMBINE_CONSUMABLE_");
                    break;
                case StateType.CombineEquipment:
                    combineConsumableCategoryButton.SetToggledOff();
                    combineEquipmentCategoryButton.SetToggledOn();
                    enhanceEquipmentCategoryButton.SetToggledOff();

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = combineEquipment.Contains;

                    combineConsumable.Hide();
                    combineEquipment.Show(true);
                    enhanceEquipment.Hide();
                    ShowSpeech("SPEECH_COMBINE_EQUIPMENT_");
                    break;
                case StateType.EnhanceEquipment:
                    combineConsumableCategoryButton.SetToggledOff();
                    combineEquipmentCategoryButton.SetToggledOff();
                    enhanceEquipmentCategoryButton.SetToggledOn();

                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    inventory.SharedModel.EffectEnabledFunc.Value = enhanceEquipment.Contains;

                    combineConsumable.Hide();
                    combineEquipment.Hide();
                    enhanceEquipment.Show(true);
                    ShowSpeech("SPEECH_COMBINE_ENHANCE_EQUIPMENT_");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
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

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            Close();
            Find<Menu>().ShowRoom();
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

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel,
            IEnumerable<(Material material, int count)> materialInfoList)
        {
            States.Instance.AgentState.Value.gold -= combinationPanel.CostNCG;
            States.Instance.CurrentAvatarState.Value.actionPoint -= combinationPanel.CostAP;
            ReactiveCurrentAvatarState.ActionPoint.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.actionPoint);
            foreach (var (material, count) in materialInfoList)
            {
                States.Instance.CurrentAvatarState.Value.inventory.RemoveFungibleItem(material, count);
            }

            ReactiveCurrentAvatarState.Inventory.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.inventory);
        }

        private void UpdateCurrentAvatarState(ICombinationPanel combinationPanel, Guid baseItemGuid,
            IEnumerable<Guid> otherItemGuidList)
        {
            States.Instance.AgentState.Value.gold -= combinationPanel.CostNCG;
            States.Instance.CurrentAvatarState.Value.actionPoint -= combinationPanel.CostAP;
            ReactiveCurrentAvatarState.ActionPoint.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.actionPoint);
            States.Instance.CurrentAvatarState.Value.inventory.RemoveNonFungibleItem(baseItemGuid);
            foreach (var itemGuid in otherItemGuidList)
            {
                States.Instance.CurrentAvatarState.Value.inventory.RemoveNonFungibleItem(itemGuid);
            }

            ReactiveCurrentAvatarState.Inventory.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.inventory);
        }

        private void CreateCombinationAction(List<(Material material, int count)> materialInfoList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_COMBINATION_START");
            Notification.Push(MailType.Workshop, msg);
            ActionManager.instance.Combination(materialInfoList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
        }

        private void CreateItemEnhancementAction(Guid baseItemGuid, IEnumerable<Guid> otherItemGuidList)
        {
            var msg = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START");
            Notification.Push(MailType.Workshop, msg);
            ActionManager.instance.ItemEnhancement(baseItemGuid, otherItemGuidList)
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
        }

        #endregion

        private void ShowSpeech(string key, CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        {
            if (_npc)
            {
                if (type == CharacterAnimation.Type.Greeting)
                {
                    _npc.Greeting();
                }
                else
                {
                    _npc.Emotion();
                }
                if (speechBubble.gameObject.activeSelf)
                    return;
                speechBubble.SetKey(key);
                StartCoroutine(speechBubble.CoShowText());
            }
        }
    }
}
