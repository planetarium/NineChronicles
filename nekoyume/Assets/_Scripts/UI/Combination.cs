using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.Manager;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        public NormalButton combineEquipmentButton;
        public NormalButton combineConsumableButton;
        public NormalButton recipeButton;

        public Module.Inventory inventory;
        public GameObject manualCombination;
        public Text materialsTitleText;
        public CombinationMaterialView equipmentMaterialView;
        public CombinationMaterialView[] materialViews;
        public GameObject materialViewsPlusImageContainer;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Text combinationButtonText;
        public GameObject recipeCombination;
        public Button recipeCloseButton;
        public Recipe recipe;
        public TextMeshProUGUI requiredPointText;

        private Stage _stage;
        private Player _player;

        public Model.Combination SharedModel { get; private set; }

        private SimpleItemCountPopup SimpleItemCountPopup { get; set; }

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

            materialsTitleText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
            combinationButtonText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");

            SimpleItemCountPopup = Find<SimpleItemCountPopup>();

            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.RecipeEnabled.Subscribe(SubscribeRecipeEnabled).AddTo(gameObject);
            SharedModel.ItemCountPopup.Value.Item.Subscribe(SubscribeItemPopup).AddTo(gameObject);
            SharedModel.ItemCountPopup.Value.OnClickCancel.Subscribe(SubscribeItemPopupOnClickCancel)
                .AddTo(gameObject);
            SharedModel.EquipmentMaterial.Subscribe(equipmentMaterialView.SetData).AddTo(gameObject);
            SharedModel.Materials.ObserveAdd().Subscribe(SubscribeMaterialAdd).AddTo(gameObject);
            SharedModel.Materials.ObserveRemove().Subscribe(SubscribeMaterialRemove).AddTo(gameObject);
            SharedModel.Materials.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(gameObject);
            SharedModel.Materials.ObserveReset().Subscribe(_ => UpdateStagedItems()).AddTo(gameObject);
            SharedModel.ShowMaterialsCount.Subscribe(SubscribeShowMaterialsCount).AddTo(gameObject);
            SharedModel.ReadyToCombination.Subscribe(SubscribeReadyToCombination).AddTo(gameObject);
            SharedModel.OnMaterialAdded.Subscribe(materialId => SubscribeOnMaterial(materialId, true))
                .AddTo(gameObject);
            SharedModel.OnMaterialRemoved.Subscribe(materialId => SubscribeOnMaterial(materialId, false))
                .AddTo(gameObject);

            combineEquipmentButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedModel.State.Value = ItemType.Equipment;
                })
                .AddTo(gameObject);
            combineConsumableButton.button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SharedModel.State.Value = ItemType.Consumable;
                })
                .AddTo(gameObject);
            recipeButton.button.OnClickAsObservable()
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
        }

        public override void Show()
        {
            base.Show();

            inventory.SharedModel.State.Value = ItemType.Material;

            _stage.LoadBackground("combination");
            _player = _stage.GetPlayer();
            _player.gameObject.SetActive(false);

            Find<BottomMenu>().Show(UINavigator.NavigationType.Back, SubscribeBackButtonClick);

            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            foreach (var item in materialViews)
            {
                item.Clear();
            }

            base.Close(ignoreCloseAnimation);

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
                    combineEquipmentButton.button.interactable = true;
                    combineConsumableButton.button.interactable = false;
                    equipmentMaterialView.gameObject.SetActive(false);
                    materialViewsPlusImageContainer.SetActive(false);
                    break;
                case ItemType.Equipment:
                    inventory.SharedModel.DimmedFunc.Value = DimmedFuncForEquipments;
                    combineEquipmentButton.button.interactable = false;
                    combineConsumableButton.button.interactable = true;
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
            SharedModel.ItemCountPopup.Value.Item.Value = null;
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
                combinationButtonText.color = Color.white;
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_gray_01");
                combinationButtonText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            Close();
            Find<Menu>().ShowRoom();
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
                if (SharedModel.EquipmentMaterial.Value is null)
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
                .Subscribe(_ => { }, _ => Find<ActionFailPopup>().Show("Timeout occurred during Combination"));
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
            Find<CombinationLoadingScreen>().Show();
            
            foreach (var material in materials)
            {
                States.Instance.currentAvatarState.Value.inventory.RemoveFungibleItem(material.ItemBase.Value.Data.Id,
                    material.Count.Value);
            }
            
            SharedModel.RemoveMaterialsAll();
            inventory.SharedModel.RemoveItems(materials);

            // 에셋의 버그 때문에 스크롤 맨 끝 포지션으로 스크롤 포지션 설정 시 스크롤이 비정상적으로 표시되는 문제가 있음.
            recipe.Reload(recipe.scrollerController.scroller.ScrollPosition - 0.1f);
        }

        private void CheckPoint(int actionPoint)
        {
            requiredPointText.color = actionPoint >= Action.Combination.RequiredPoint ? Color.white : Color.red;
        }

        public void ItemEnhancement()
        {
            var equipments = _player.Inventory.Items.Select(i => i.item).OfType<Equipment>().ToList();
            var itemId = equipments.First().ItemId;
            var materialIds = new List<Guid>
            {
                equipments[1].ItemId
            };
            ActionManager.instance.ItemEnhancement(itemId, materialIds);
        }
    }
}
