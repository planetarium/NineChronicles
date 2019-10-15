using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Stage = Nekoyume.Game.Stage;
using Assets.SimpleLocalization;
using Nekoyume.Helper;
using Nekoyume.EnumType;
using Nekoyume.Game.Factory;
using Nekoyume.Model;
using Nekoyume.State;
using TMPro;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        public Module.Inventory inventory;
        public BottomMenu bottomMenu;
        public GameObject manualCombination;
        public Text materialsTitleText;
        public CombinationMaterialView equipmentMaterialView;
        public CombinationMaterialView[] materialViews;
        public GameObject materialViewsPlusImageContainer;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Text combinationButtonText;
        public GameObject recipeCombination;
        public Button closeButton;
        public Button recipeCloseButton;
        public Recipe recipe;
        public TextMeshProUGUI requiredPointText;

        private Stage _stage;
        private Game.Character.Player _player;
        private IDisposable _disposable;

        private SimpleItemCountPopup SimpleItemCountPopup { get; set; }
        public Model.Combination SharedModel { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            _stage = Game.Game.instance.stage;
            
            SharedModel = new Model.Combination();
        }

        #endregion
        
        #region Override

        public override void Initialize()
        {
            base.Initialize();
            
            inventory.SharedModel.SelectedItemView.Subscribe(SubscribeInventorySelectedItemView);
            inventory.SharedModel.OnRightClickItemView
                .Subscribe(itemView =>
                {
                    if (itemView.Model.Dimmed.Value)
                        return;
                    
                    SharedModel.RegisterToStagedItems(itemView.Model);
                })
                .AddTo(gameObject);

            bottomMenu.combinationEquipmentButton.text.text = LocalizationManager.Localize("UI_COMBINE_EQUIPMENTS");
            bottomMenu.combinationConsumableButton.text.text = LocalizationManager.Localize("UI_COMBINE_CONSUMABLES");
            bottomMenu.combinationRecipeButton.text.text = LocalizationManager.Localize("UI_RECIPE");
            materialsTitleText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
            combinationButtonText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");

            SimpleItemCountPopup = Find<SimpleItemCountPopup>();
            
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.RecipeEnabled.Subscribe(SubscribeRecipeEnabled).AddTo(gameObject);
            SharedModel.ItemCountPopup.Value.item.Subscribe(SubscribeItemPopup).AddTo(gameObject);
            SharedModel.ItemCountPopup.Value.onClickCancel.Subscribe(SubscribeItemPopupOnClickCancel)
                .AddTo(gameObject);
            SharedModel.EquipmentMaterial.Subscribe(equipmentMaterialView.SetData).AddTo(gameObject);
            SharedModel.Materials.ObserveAdd().Subscribe(SubscribeMaterialAdd).AddTo(gameObject);
            SharedModel.Materials.ObserveRemove().Subscribe(SubscribeMaterialRemove).AddTo(gameObject);
            SharedModel.Materials.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(gameObject);
            SharedModel.ShowMaterialsCount.Subscribe(SubscribeShowMaterialsCount).AddTo(gameObject);
            SharedModel.ReadyToCombination.Subscribe(SubscribeReadyToCombination).AddTo(gameObject);
            SharedModel.OnMaterialAdded.Subscribe(materialId => SubscribeOnMaterial(materialId, true))
                .AddTo(gameObject);
            SharedModel.OnMaterialRemoved.Subscribe(materialId => SubscribeOnMaterial(materialId, false))
                .AddTo(gameObject);

            bottomMenu.combinationEquipmentButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedModel.State.Value = ItemType.Equipment;
                })
                .AddTo(gameObject);
            bottomMenu.combinationConsumableButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedModel.State.Value = ItemType.Consumable;
                })
                .AddTo(gameObject);
            bottomMenu.combinationRecipeButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();

                    SharedModel.RecipeEnabled.Value = !SharedModel.RecipeEnabled.Value;
                })
                .AddTo(gameObject);
            combinationButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    RequestCombination(SharedModel);
                })
                .AddTo(gameObject);
            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToMenu();
                })
                .AddTo(gameObject);
            recipeCloseButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedModel.RecipeEnabled.Value = false;
                })
                .AddTo(gameObject);
            recipe.scrollerController.onClickCellView
                .Subscribe(cellView =>
                {
                    AudioController.PlayClick();
                    RequestCombination(cellView.Model);
                })
                .AddTo(gameObject);

            UpdateStagedItems();

            bottomMenu.goToMainButton.button.onClick.AddListener(GoToMenu);
            var status = Find<Status>();
            bottomMenu.questButton.button.onClick.AddListener(status.ToggleQuest);
            requiredPointText.text = Action.Combination.RequiredPoint.ToString();
        }

        public override void Show()
        {
            base.Show();
            
            inventory.SharedModel.State.Value = ItemType.Material;

            _stage.LoadBackground("combination");
            _player = _stage.GetPlayer();
            _player.gameObject.SetActive(false);
            _disposable = ReactiveCurrentAvatarState.ActionPoint.Subscribe(CheckPoint);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close()
        {
            foreach (var item in materialViews)
            {
                item.Clear();
            }

            base.Close();
            _disposable.Dispose();

            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        #endregion

        #region Subscribe

        private void SubscribeInventorySelectedItemView(InventoryItemView view)
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
                value => !view.Model.Dimmed.Value,
                LocalizationManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                tooltip =>
                {
                    SharedModel.RegisterToStagedItems(tooltip.itemInformation.Model.item.Value);
                    inventory.Tooltip.Close();
                },
                tooltip => inventory.SharedModel.DeselectItemView());
        }

        private void SubscribeState(ItemType value)
        {
            switch (value)
            {
                case ItemType.Consumable:
                    inventory.SharedModel.DimmedFunc.Value = DimmedFuncForConsumables;
                    bottomMenu.combinationEquipmentButton.button.interactable = true;
                    bottomMenu.combinationConsumableButton.button.interactable = false;
                    equipmentMaterialView.gameObject.SetActive(false);
                    materialViewsPlusImageContainer.SetActive(false);
                    break;
                case ItemType.Equipment:
                    inventory.SharedModel.DimmedFunc.Value = DimmedFuncForEquipments;
                    bottomMenu.combinationEquipmentButton.button.interactable = false;
                    bottomMenu.combinationConsumableButton.button.interactable = true;
                    equipmentMaterialView.gameObject.SetActive(true);
                    materialViewsPlusImageContainer.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            inventory.Tooltip.Close();
        }

        private void SubscribeRecipeEnabled(bool value)
        {
            if (value)
            {
                recipe.Reload();
            }

            manualCombination.SetActive(!value);
            recipeCombination.SetActive(value);
        }

        private void SubscribeOnMaterial(int materialId, bool isAdded)
        {
            foreach (var item in inventory.SharedModel.Materials)
            {
                if (item.ItemBase.Value.Data.Id != materialId)
                {
                    continue;
                }

                item.Covered.Value = isAdded;
                item.Dimmed.Value = isAdded;

                inventory.SharedModel.DimmedFunc.SetValueAndForceNotify(inventory.SharedModel.DimmedFunc.Value);

                break;
            }
        }

        private void SubscribeItemPopup(CountableItem data)
        {
            if (ReferenceEquals(data, null))
            {
                SimpleItemCountPopup.Close();
                return;
            }

            SimpleItemCountPopup.Pop(SharedModel.ItemCountPopup.Value);
        }

        private void SubscribeItemPopupOnClickCancel(Model.SimpleItemCountPopup data)
        {
            SharedModel.ItemCountPopup.Value.item.Value = null;
            SimpleItemCountPopup.Close();
        }

        private void SubscribeMaterialAdd(CollectionAddEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                SharedModel.Materials.RemoveAt(e.Index);
                throw new AddOutOfSpecificRangeException<CollectionAddEvent<CountEditableItem>>(
                    materialViews.Length);
            }

            materialViews[e.Index].SetData(e.Value);
        }

        private void SubscribeMaterialRemove(CollectionRemoveEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                return;
            }

            var dataCount = SharedModel.Materials.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = materialViews[i];

                if (i < dataCount)
                {
                    item.SetData(SharedModel.Materials[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void SubscribeShowMaterialsCount(int value)
        {
            for (var i = 0; i < materialViews.Length; i++)
            {
                materialViews[i].gameObject.SetActive(i < value);
            }
        }

        private void SubscribeReadyToCombination(bool isActive)
        {
            if (isActive)
            {
                combinationButton.enabled = true;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                combinationButtonText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }

        #endregion

        private static bool DimmedFuncForConsumables(InventoryItem inventoryItem)
        {
            var row = inventoryItem.ItemBase.Value.Data;
            return row.ItemType != ItemType.Material ||
                   row.ItemSubType != ItemSubType.FoodMaterial;
        }

        private static bool DimmedFuncForEquipments(InventoryItem inventoryItem)
        {
            var row = inventoryItem.ItemBase.Value.Data;
            return row.ItemType != ItemType.Material ||
                   row.ItemSubType != ItemSubType.EquipmentMaterial &&
                   row.ItemSubType != ItemSubType.MonsterPart;
        }

        private void UpdateStagedItems(int startIndex = 0)
        {
            if (SharedModel.State.Value == ItemType.Equipment)
            {
                if (SharedModel.EquipmentMaterial.Value == null)
                {
                    equipmentMaterialView.Clear();
                }
                else
                {
                    equipmentMaterialView.SetData(SharedModel.EquipmentMaterial.Value);
                }
            }

            var dataCount = SharedModel.Materials.Count;
            for (var i = startIndex; i < materialViews.Length; i++)
            {
                var item = materialViews[i];
                if (i < dataCount)
                {
                    item.SetData(SharedModel.Materials[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void RequestCombination(Model.Combination data)
        {
            var materials = new List<CombinationMaterial>();
            if (data.EquipmentMaterial.Value != null)
            {
                materials.Add(data.EquipmentMaterial.Value);
            }

            materials.AddRange(data.Materials);

            RequestCombination(materials);
        }

        private void RequestCombination(RecipeInfo info)
        {
            var materials = info.materialInfos
                .Where(materialInfo => materialInfo.id != 0)
                .Select(materialInfo => new CombinationMaterial(
                    ItemFactory.CreateMaterial(materialInfo.id), 1, 1, 1))
                .ToList();

            RequestCombination(materials);
        }

        private void RequestCombination(List<CombinationMaterial> materials)
        {
            //게임상의 액션포인트 업데이트
            var newState = (AvatarState) States.Instance.currentAvatarState.Value.Clone();
            newState.actionPoint -= Action.Combination.RequiredPoint;
            var index = States.Instance.currentAvatarKey.Value;
            ActionRenderHandler.UpdateLocalAvatarState(newState, index);

            ActionManager.instance.Combination(materials)
                .Subscribe((_) => { }, (_) => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
            Find<CombinationLoadingScreen>().Show();
            inventory.SharedModel.RemoveItems(materials);
            SharedModel.RemoveEquipmentMaterial();
            foreach (var material in materials)
            {
                States.Instance.currentAvatarState.Value.inventory.RemoveFungibleItem(material.ItemBase.Value.Data.Id,
                    material.Count.Value);
            }

            while (SharedModel.Materials.Count > 0)
            {
                SharedModel.Materials.RemoveAt(0);
            }

            // 에셋의 버그 때문에 스크롤 맨 끝 포지션으로 스크롤 포지션 설정 시 스크롤이 비정상적으로 표시되는 문제가 있음.
            recipe.Reload(recipe.scrollerController.scroller.ScrollPosition - 0.1f);
        }

        private void GoToMenu()
        {
            Close();
            Find<Menu>().ShowRoom();
        }

        private void CheckPoint(int actionPoint)
        {
            requiredPointText.color = actionPoint >= Action.Combination.RequiredPoint ? Color.white : Color.red;
        }
    }
}
