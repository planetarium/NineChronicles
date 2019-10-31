using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        public enum StateType
        {
            CombineConsumable,
            CombineEquipment,
            EnhanceEquipment
        }

        public readonly ReactiveProperty<StateType> State =
            new ReactiveProperty<StateType>(StateType.CombineEquipment);

        public CategoryButton combineConsumableCategoryButton;
        public CategoryButton combineEquipmentCategoryButton;
        public CategoryButton enhanceEquipmentCategoryButton;

        public Module.Inventory inventory;

        public CombineConsumable combineConsumable;
        public CombineEquipment combineEquipment;
        public EnhanceEquipment enhanceEquipment;

        #region Override

        public override void Initialize()
        {
            base.Initialize();

            State.Subscribe(SubscribeState).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.SharedModel.OnRightClickItemView.Subscribe(StageMaterial).AddTo(gameObject);
            
            combineConsumable.RemoveMaterialsAll();
            combineConsumable.OnMaterialAdd.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            combineConsumable.OnMaterialRemove.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            combineConsumable.OnSubmitClick.Subscribe(_ => ActionCombineConsumable()).AddTo(gameObject);
            combineConsumable.recipe.scrollerController.OnSubmitClick.Subscribe(ActionCombineConsumable)
                .AddTo(gameObject);
            
            combineEquipment.RemoveMaterialsAll();
            combineEquipment.OnMaterialAdd.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            combineEquipment.OnMaterialRemove.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            combineEquipment.OnSubmitClick.Subscribe(_ => ActionCombineEquipment()).AddTo(gameObject);
            
            enhanceEquipment.RemoveMaterialsAll();
            enhanceEquipment.OnMaterialAdd.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            enhanceEquipment.OnMaterialRemove.Subscribe(SubscribeOnMaterialAddOrRemove).AddTo(gameObject);
            enhanceEquipment.OnSubmitClick.Subscribe(_ => ActionEnhanceEquipment()).AddTo(gameObject);

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

            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            combineConsumable.RemoveMaterialsAll();
            combineEquipment.RemoveMaterialsAll();
            enhanceEquipment.RemoveMaterialsAll();

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void SubscribeState(StateType value)
        {
            inventory.Tooltip.Close();
            inventory.SharedModel.DeselectItemView();

            switch (value)
            {
                case StateType.CombineConsumable:
                    combineConsumableCategoryButton.SetToggledOn();
                    combineEquipmentCategoryButton.SetToggledOff();
                    enhanceEquipmentCategoryButton.SetToggledOff();

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineConsumable.DimFunc;
                    
                    combineConsumable.Show();
                    combineEquipment.Hide();
                    enhanceEquipment.Hide();
                    break;
                case StateType.CombineEquipment:
                    combineConsumableCategoryButton.SetToggledOff();
                    combineEquipmentCategoryButton.SetToggledOn();
                    enhanceEquipmentCategoryButton.SetToggledOff();

                    inventory.SharedModel.State.Value = ItemType.Material;
                    inventory.SharedModel.DimmedFunc.Value = combineEquipment.DimFunc;
                    
                    combineConsumable.Hide();
                    combineEquipment.Show();
                    enhanceEquipment.Hide();
                    break;
                case StateType.EnhanceEquipment:
                    combineConsumableCategoryButton.SetToggledOff();
                    combineEquipmentCategoryButton.SetToggledOff();
                    enhanceEquipmentCategoryButton.SetToggledOn();

                    inventory.SharedModel.State.Value = ItemType.Equipment;
                    inventory.SharedModel.DimmedFunc.Value = enhanceEquipment.DimFunc;
                    
                    combineConsumable.Hide();
                    combineEquipment.Hide();
                    enhanceEquipment.Show();
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
        private void SubscribeOnMaterialAddOrRemove(InventoryItem viewModel)
        {   
            inventory.SharedModel.UpdateDimAll();
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
                .Where(e => !(e is null) && !(e.Model is null))
                .Select(e => (e.Model.ItemBase.Value.Data.Id, e.Model.Count.Value))
                .ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList);
            combineConsumable.RemoveMaterialsAll();
        }

        private void ActionCombineConsumable(RecipeCellView view)
        {
            var materialInfoList = view.Model.MaterialInfos
                .Where(materialInfo => materialInfo.Id != 0)
                .Select(materialInfo => (materialInfo.Id, materialInfo.Amount))
                .ToList();

            UpdateCurrentAvatarState(combineConsumable, materialInfoList);
            CreateCombinationAction(materialInfoList);
            
            // 에셋의 버그 때문에 스크롤 맨 끝 포지션으로 스크롤 포지션 설정 시 스크롤이 비정상적으로 표시되는 문제가 있음.
            var scroller = combineConsumable.recipe.scrollerController.scroller; 
            scroller.ReloadData(scroller.ScrollPosition - 0.1f);
        }

        private void ActionCombineEquipment()
        {
            var materialInfoList = new List<(int id, int value)>();
            materialInfoList.Add((
                combineEquipment.baseMaterial.Model.ItemBase.Value.Data.Id,
                combineEquipment.baseMaterial.Model.Count.Value));
            materialInfoList.AddRange(combineEquipment.otherMaterials
                .Where(e => !(e is null) && !(e.Model is null))
                .Select(e => (e.Model.ItemBase.Value.Data.Id, e.Model.Count.Value)));

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
            IEnumerable<(int itemId, int count)> materialInfoList)
        {
            States.Instance.AgentState.Value.gold -= combinationPanel.CostNCG;
            States.Instance.CurrentAvatarState.Value.actionPoint -= combinationPanel.CostAP;
            ReactiveCurrentAvatarState.ActionPoint.SetValueAndForceNotify(
                States.Instance.CurrentAvatarState.Value.actionPoint);
            foreach (var (itemId, count) in materialInfoList)
            {
                States.Instance.CurrentAvatarState.Value.inventory.RemoveFungibleItem(itemId, count);
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

        private void CreateCombinationAction(List<(int itemId, int count)> materialInfoList)
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
    }
}
