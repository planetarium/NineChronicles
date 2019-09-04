using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public enum State
        {
            Equipments, Consumables, Materials
        }
        
        public readonly ReactiveProperty<State> state = new ReactiveProperty<State>(State.Equipments);
        public readonly ReactiveCollection<InventoryItem> equipments = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> consumables = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> materials = new ReactiveCollection<InventoryItem>();
        
        public readonly ReactiveProperty<InventoryItemView> selectedItemView = new ReactiveProperty<InventoryItemView>(null);
        public readonly ReactiveProperty<Func<InventoryItem, bool>> dimmedFunc = new ReactiveProperty<Func<InventoryItem, bool>>();
        public readonly ReactiveProperty<Func<InventoryItem, bool>> equippedFunc = new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly Subject<InventoryItemView> onDoubleClickItemView = new Subject<InventoryItemView>();
        public readonly Subject<InventoryItemView> onRightClickItemView = new Subject<InventoryItemView>();
        
        public Inventory(Game.Item.Inventory inventory, State state = State.Equipments)
        {
            this.state.Value = state;
            dimmedFunc.Value = DimmedFunc;
            
            UpdateItems(inventory.Items);

            equipments.ObserveAdd().Subscribe(added => InitInventoryItem(added.Value));
            equipments.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            consumables.ObserveAdd().Subscribe(added => InitInventoryItem(added.Value));
            consumables.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            materials.ObserveAdd().Subscribe(added => InitInventoryItem(added.Value));
            materials.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            
            dimmedFunc.Subscribe(SubscribeDimmedFunc);
        }

        public void Dispose()
        {
            state.Dispose();
            equipments.Dispose();
            consumables.Dispose();
            materials.Dispose();
            selectedItemView.Dispose();
            dimmedFunc.Dispose();
            
            onDoubleClickItemView.Dispose();
            onRightClickItemView.Dispose();
        }
        
        public InventoryItem AddItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase)
            {
                case Equipment equipment:
                    return AddItem(equipment);
                case Food consumable:
                    return AddItem(consumable);
                case Material material:
                    return AddItem(material, count);
                default:
                    return null;
            }
        }
        
        public InventoryItem AddItem(Equipment equipment)
        {
            var inventoryItem = new InventoryItem(equipment, 1);
            equipments.Add(inventoryItem);
            return inventoryItem;
        }
        
        public InventoryItem AddItem(Food consumable)
        {
            var inventoryItem = new InventoryItem(consumable, 1);
            consumables.Add(inventoryItem);
            return inventoryItem;
        }
        
        public InventoryItem AddItem(Material material, int count)
        {
            if (TryGetMaterial(material, out var inventoryItem))
            {
                inventoryItem.count.Value += count;
                return inventoryItem;
            }

            inventoryItem = new InventoryItem(material, count);
            materials.Add(inventoryItem);
            return inventoryItem;
        }
        
        public bool RemoveItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase)
            {
                case Equipment equipment:
                    return RemoveItem(equipment);
                case Food consumable:
                    return RemoveItem(consumable);
                case Material material:
                    return RemoveItem(material, count);
                default:
                    return false;
            }
        }
        
        public bool RemoveItem(Equipment equipment)
        {
            return TryGetEquipment(equipment, out var inventoryItem) && equipments.Remove(inventoryItem);
        }
        
        public bool RemoveItem(Food consumable)
        {
            return TryGetConsumable(consumable, out var inventoryItem) && consumables.Remove(inventoryItem);
        }

        public bool RemoveItem(Material material, int count)
        {
            if (!TryGetMaterial(material, out var inventoryItem) ||
                inventoryItem.count.Value < count)
            {
                return false;
            }

            inventoryItem.count.Value -= count;
            if (inventoryItem.count.Value == 0)
            {
                materials.Remove(inventoryItem);
            }

            return true;
        }

        public void RemoveItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (ReferenceEquals(countEditableItem, null))
                {
                    continue;
                }

                RemoveItem(countEditableItem.item.Value, countEditableItem.count.Value);
            }
        }

        public bool TryGetItem(string guid, out ItemUsable itemUsable)
        {
            foreach (var inventoryItem in equipments)
            {
                if (!(inventoryItem.item.Value is Equipment equipment)
                    || !equipment.ItemId.Equals(guid))
                {
                    continue;
                }
                
                itemUsable = equipment;
                return true;
            }

            itemUsable = null;
            return false;
        }
        
        public bool TryGetEquipment(ItemUsable itemUsable, out InventoryItem inventoryItem)
        {
            if (itemUsable is null)
            {
                inventoryItem = null;
                return false;
            }
            
            foreach (var item in equipments)
            {
                if (!(item.item.Value is Equipment equipment))
                {
                    continue;
                }

                if (equipment.ItemId != itemUsable.ItemId)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }
        
        public bool TryGetConsumable(Food consumable, out InventoryItem inventoryItem)
        {
            if (consumable is null)
            {
                inventoryItem = null;
                return false;
            }

            foreach (var item in consumables)
            {
                if (!(item.item.Value is Food food))
                {
                    continue;
                }

                if (food.ItemId != consumable.ItemId)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }
            inventoryItem = null;
            return false;
        }
        
        public bool TryGetMaterial(Material material, out InventoryItem inventoryItem)
        {
            foreach (var item in materials)
            {
                if (item.item.Value.Data.id != material.Data.id)
                {
                    continue;
                }
                
                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }
        
        public void DeselectAll()
        {
            if (selectedItemView.Value == null
                || selectedItemView.Value.Model == null)
            {
                return;
            }
            selectedItemView.Value.Model.selected.Value = false;
            selectedItemView.Value = null;
        }

        private void SubscribeDimmedFunc(Func<InventoryItem,bool> func)
        {
            if (dimmedFunc.Value == null)
            {
                dimmedFunc.Value = DimmedFunc;
            }
                
            foreach (var item in equipments)
            {
                item.dimmed.Value = dimmedFunc.Value(item);
            }
            foreach (var item in consumables)
            {
                item.dimmed.Value = dimmedFunc.Value(item);
            }
            foreach (var item in materials)
            {
                item.dimmed.Value = dimmedFunc.Value(item);
            }
        }

        private void SubscribeOnClick(InventoryItemView view)
        {
            var prevSelected = selectedItemView.Value;
            if (selectedItemView.Value != null)
            {
                DeselectAll();
            }
            if (prevSelected is null || !ReferenceEquals(prevSelected, view))
            {
                SelectView(view);
            }
        }

        private void SelectView(InventoryItemView view)
        {
            selectedItemView.SetValueAndForceNotify(view);
            selectedItemView.Value.Model.selected.Value = true;
            SetGlowedAll(false);
        }

        private void UpdateItems(IEnumerable<Game.Item.Inventory.Item> list)
        {
            foreach (var item in list)
            {
                var inventoryItem = new InventoryItem(item.item, item.count);
                InitInventoryItem(inventoryItem);
                
                switch (item.item)
                {
                    case Equipment equipment:
                        inventoryItem.equipped.Value = equipment.equipped;
                        equipments.Add(inventoryItem);
                        break;
                    case Food _:
                        consumables.Add(inventoryItem);
                        break;
                    default:
                        materials.Add(inventoryItem);
                        break;
                }
            }
            
            // 정렬.
        }

        private void InitInventoryItem(InventoryItem item)
        {
            item.dimmed.Value = dimmedFunc.Value(item);
            item.onClick.Subscribe(SubscribeOnClick);
            item.onDoubleClick.Subscribe(onDoubleClickItemView);
            item.onRightClick.Subscribe(onRightClickItemView);
        }
        
        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in equipments)
            {
                item.glowed.Value = value;
            }
            
            foreach (var item in consumables)
            {
                item.glowed.Value = value;
            }
            
            foreach (var item in materials)
            {
                item.glowed.Value = value;
            }
        }
    }
}
