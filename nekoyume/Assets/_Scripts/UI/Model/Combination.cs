using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Manager;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        public enum ConsumablesOrEquipments
        {
            Consumables,
            Equipments
        }

        public enum ManualOrRecipe
        {
            Manual,
            Recipe
        }

        private static readonly string[] DimmedTypes =
        {
            nameof(ItemBase.ItemType.Weapon),
            nameof(ItemBase.ItemType.RangedWeapon),
            nameof(ItemBase.ItemType.Armor),
            nameof(ItemBase.ItemType.Belt),
            nameof(ItemBase.ItemType.Necklace),
            nameof(ItemBase.ItemType.Ring),
            nameof(ItemBase.ItemType.Helm),
            nameof(ItemBase.ItemType.Set),
            nameof(ItemBase.ItemType.Food),
            nameof(ItemBase.ItemType.Shoes)
        };
        
        public readonly ReactiveProperty<ConsumablesOrEquipments> consumablesOrEquipments =
            new ReactiveProperty<ConsumablesOrEquipments>(ConsumablesOrEquipments.Equipments);

        public readonly ReactiveProperty<ManualOrRecipe> manualOrRecipe =
            new ReactiveProperty<ManualOrRecipe>(ManualOrRecipe.Manual);

        public readonly ReactiveProperty<Inventory> inventory
            = new ReactiveProperty<Inventory>();

        public readonly ReactiveProperty<SimpleItemCountPopup> itemCountPopup =
            new ReactiveProperty<SimpleItemCountPopup>();

        public readonly ReactiveProperty<CombinationMaterial> equipmentMaterial =
            new ReactiveProperty<CombinationMaterial>();
        
        public readonly ReactiveCollection<CombinationMaterial> materials =
            new ReactiveCollection<CombinationMaterial>();

        public readonly ReactiveProperty<int> showMaterialsCount = new ReactiveProperty<int>();
        public readonly ReactiveProperty<bool> readyForCombination = new ReactiveProperty<bool>();

        public readonly ReactiveProperty<CombinationResultPopup> resultPopup =
            new ReactiveProperty<CombinationResultPopup>();

        public readonly Subject<CombinationResultPopup> onShowResultVFX = new Subject<CombinationResultPopup>();

        public Combination(Game.Item.Inventory inventory)
        {
            this.inventory.Value = new Inventory(inventory, Inventory.State.Materials);
            itemCountPopup.Value = new SimpleItemCountPopup();
            itemCountPopup.Value.titleText.Value =
                LocalizationManager.Localize("UI_COMBINATION_MATERIAL_COUNT_SELECTION");

            consumablesOrEquipments.Subscribe(Subscribe);
            itemCountPopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountPopup);
            materials.ObserveAdd().Subscribe(_ => OnMaterialAdd(_.Value));
            materials.ObserveRemove().Subscribe(_ => OnMaterialRemove(_.Value));
            resultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            consumablesOrEquipments.Dispose();
            manualOrRecipe.Dispose();
            inventory.DisposeAll();
            itemCountPopup.DisposeAll();
            equipmentMaterial.Dispose();
            materials.DisposeAll();
            showMaterialsCount.Dispose();
            readyForCombination.Dispose();
            resultPopup.DisposeAll();

            onShowResultVFX.Dispose();
        }

        private void Subscribe(ConsumablesOrEquipments value)
        {
            switch (value)
            {
                case ConsumablesOrEquipments.Consumables:
                    inventory.Value.dimmedFunc.Value = DimmedFuncForConsumables;
                    showMaterialsCount.Value = 5;
                    break;
                case ConsumablesOrEquipments.Equipments:
                    inventory.Value.dimmedFunc.Value = DimmedFuncForEquipments;
                    showMaterialsCount.Value = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            RemoveEquipmentMaterial();
            
            while (materials.Count > 0)
            {
                materials.RemoveAt(0);
            }
        }
        
        private bool DimmedFuncForConsumables(InventoryItem inventoryItem)
        {
            return DimmedTypes.Contains(inventoryItem.item.Value.Data.cls)
                   || GameConfig.PaintMaterials.Contains(inventoryItem.item.Value.Data.id)
                   || !GameConfig.ConsumableMaterials.Contains(inventoryItem.item.Value.Data.id);
        }
        
        private bool DimmedFuncForEquipments(InventoryItem inventoryItem)
        {
            return DimmedTypes.Contains(inventoryItem.item.Value.Data.cls)
                   || GameConfig.PaintMaterials.Contains(inventoryItem.item.Value.Data.id)
                   || GameConfig.ConsumableMaterials.Contains(inventoryItem.item.Value.Data.id);
        }

        private void OnClickSubmitItemCountPopup(SimpleItemCountPopup data)
        {
            if (ReferenceEquals(data, null)
                || ReferenceEquals(data.item.Value, null))
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            RegisterToStagedItems(data.item.Value);
            itemCountPopup.Value.item.Value = null;
        }
        
        private void RemoveEquipmentMaterial()
        {
            if (equipmentMaterial.Value == null)
            {
                return;
            }
            
            var temp = equipmentMaterial.Value;
            equipmentMaterial.Value = null;
            OnMaterialRemove(temp);
        }

        public bool RegisterToStagedItems(CountableItem countEditableItem)
        {
            if (ReferenceEquals(countEditableItem, null))
            {
                return false;
            }

            int sum = materials
                .Where(item => countEditableItem.item.Value.Data.id == item.item.Value.Data.id)
                .Sum(item => item.count.Value);

            if (sum >= countEditableItem.count.Value)
            {
                return false;
            }

            if (consumablesOrEquipments.Value == ConsumablesOrEquipments.Equipments
                && GameConfig.EquipmentMaterials.Contains(countEditableItem.item.Value.Data.id))
            {
                RemoveEquipmentMaterial();
                
                equipmentMaterial.Value = new CombinationMaterial(
                    countEditableItem.item.Value,
                    1,
                    0,
                    countEditableItem.count.Value);
                OnMaterialAdd(equipmentMaterial.Value);
                
                return true;
            }

            foreach (var material in materials)
            {
                if (material.item.Value.Data.id != 0)
                {
                    continue;
                }

                if (countEditableItem.count.Value == 0)
                {
                    materials.Remove(material);
                }

                return true;
            }

            if (materials.Count >= showMaterialsCount.Value)
            {
                return false;
            }

            materials.Add(new CombinationMaterial(
                countEditableItem.item.Value,
                1,
                0,
                countEditableItem.count.Value));
            
            return true;
        }

        private void OnMaterialAdd(CombinationMaterial value)
        {
            value.count.Subscribe(count => UpdateReadyForCombination());
            value.onMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                if (obj.count.Value > 1)
                {
                    obj.count.Value--;
                }
            });
            value.onPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                int sum = materials
                .Where(item => obj.item.Value.Data.id == item.item.Value.Data.id)
                .Sum(item => item.count.Value);
                if (sum < obj.maxCount.Value)
                {
                    obj.count.Value++;
                }
            });
            value.onDelete.Subscribe(obj =>
            {
                if (!(obj is CombinationMaterial material))
                {
                    return;
                }

                if (consumablesOrEquipments.Value == ConsumablesOrEquipments.Equipments
                    && equipmentMaterial.Value != null
                    && equipmentMaterial.Value.item.Value.Data.id == obj.item.Value.Data.id)
                {
                    RemoveEquipmentMaterial();
                }
                else
                {
                    materials.Remove(material);   
                }
                
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationRemoveMaterialItem);
            });

            SetStaged(value.item.Value.Data.id, true);
            UpdateReadyForCombination();
        }

        private void OnMaterialRemove(CombinationMaterial value)
        {
            value.Dispose();

            bool exists = materials.Any(item => item.item.Value.Data.id == value.item.Value.Data.id);
            SetStaged(value.item.Value.Data.id, exists);
            UpdateReadyForCombination();
        }

        private void SetStaged(int materialId, bool isStaged)
        {
            foreach (var item in inventory.Value.materials)
            {
                if (item.item.Value.Data.id != materialId)
                {
                    continue;
                }

                item.covered.Value = isStaged;
                item.dimmed.Value = isStaged;
                
                inventory.Value.dimmedFunc.SetValueAndForceNotify(inventory.Value.dimmedFunc.Value);

                break;
            }
        }

        private void UpdateReadyForCombination()
        {
            switch (consumablesOrEquipments.Value)
            {
                case ConsumablesOrEquipments.Consumables:
                    readyForCombination.Value = materials.Count >= 2;
                    break;
                case ConsumablesOrEquipments.Equipments:
                    readyForCombination.Value = 
                        equipmentMaterial.Value != null
                        && materials.Count >= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void OnResultPopup(CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            resultPopup.Value.onClickSubmit.Subscribe(OnResultPopupOnClickSubmit);
        }

        private void OnResultPopupOnClickSubmit(CombinationResultPopup data)
        {
            // 재료 아이템들을 인벤토리에서 제거하기.
            inventory.Value.RemoveItems(data.materialItems);

            // 결과 아이템이 있다면, 인벤토리에 추가하고 해당 아이템을 선택하기.
            if (!ReferenceEquals(data.itemInformation.Value.item.Value, null))
            {
                inventory.Value.AddItem((ItemUsable) data.itemInformation.Value.item.Value.item.Value);
            }
            
            RemoveEquipmentMaterial();
            while (materials.Count > 0)
            {
                materials.RemoveAt(0);
            }

            onShowResultVFX.OnNext(resultPopup.Value);

            resultPopup.Value.Dispose();
            resultPopup.Value = null;
        }
    }
}
