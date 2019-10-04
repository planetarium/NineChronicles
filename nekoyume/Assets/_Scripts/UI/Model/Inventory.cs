using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveProperty<ItemType> State = new ReactiveProperty<ItemType>(ItemType.Equipment);
        public readonly ReactiveCollection<InventoryItem> Consumables = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> Equipments = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveCollection<InventoryItem> Materials = new ReactiveCollection<InventoryItem>();

        public readonly ReactiveProperty<InventoryItemView> SelectedItemView =
            new ReactiveProperty<InventoryItemView>(null);

        public readonly ReactiveProperty<Func<InventoryItem, bool>> DimmedFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EquippedFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly Subject<InventoryItemView> OnRightClickItemView = new Subject<InventoryItemView>();

        public Inventory(ItemType stateType = ItemType.Equipment)
        {
            State.Value = stateType;
            DimmedFunc.Value = DefaultDimmedFunc;

            State.Subscribe(SubscribeState);
            DimmedFunc.Subscribe(SubscribeDimmedFunc);
        }

        public void Dispose()
        {
            State.Dispose();
            Consumables.Dispose();
            Equipments.Dispose();
            Materials.Dispose();
            SelectedItemView.Dispose();
            DimmedFunc.Dispose();
            EquippedFunc.Dispose();
            OnRightClickItemView.Dispose();
        }

        public void ResetItems(Game.Item.Inventory inventory)
        {
            Debug.LogWarning($"Model.Inventory.ResetItems() called. inventory.Items.Count is {inventory?.Items.Count ?? 0}");
            
            RemoveItemsAll();

            if (inventory is null)
            {
                return;
            }

            foreach (var item in inventory.Items)
            {
                AddItem(item.item, item.count);
            }

            // 정렬.
            
            State.SetValueAndForceNotify(State.Value);
        }

        #region Inventory Item Pool

        private InventoryItem CreateInventoryItem(ItemBase itemBase, int count)
        {
            var item = new InventoryItem(itemBase, count);
            item.Dimmed.Value = DimmedFunc.Value(item);
            item.OnClick.Subscribe(SubscribeItemOnClick);
            item.OnRightClick.Subscribe(OnRightClickItemView);
            return item;
        }

        #endregion

        #region Add Item

        public InventoryItem AddItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase)
            {
                case Equipment equipment:
                    return AddItem(equipment);
                case Consumable consumable:
                    return AddItem(consumable);
                case Material material:
                    return AddItem(material, count);
                default:
                    return null;
            }
        }

        public InventoryItem AddItem(Equipment equipment)
        {
            var inventoryItem = CreateInventoryItem(equipment, 1);
            inventoryItem.Equipped.Value = equipment.equipped;
            Equipments.Add(inventoryItem);

            return inventoryItem;
        }

        public InventoryItem AddItem(Consumable consumable)
        {
            var inventoryItem = CreateInventoryItem(consumable, 1);
            Consumables.Add(inventoryItem);

            return inventoryItem;
        }

        public InventoryItem AddItem(Material material, int count)
        {
            if (TryGetMaterial(material, out var inventoryItem))
            {
                inventoryItem.Count.Value += count;
                return inventoryItem;
            }

            inventoryItem = CreateInventoryItem(material, count);
            Materials.Add(inventoryItem);

            return inventoryItem;
        }

        #endregion

        #region Remove Item

        public void RemoveItem(ItemBase itemBase, int count = 1)
        {
            InventoryItem item = null;
            switch (itemBase)
            {
                case Equipment equipment:
                    item = RemoveItem(equipment);
                    break;
                case Consumable consumable:
                    item = RemoveItem(consumable);
                    break;
                case Material material:
                    item = RemoveItem(material, count);
                    break;
            }
            
            item?.Dispose();
        }

        public InventoryItem RemoveItem(Equipment equipment)
        {
            if (!TryGetEquipment(equipment, out var inventoryItem))
                return null;

            Equipments.Remove(inventoryItem);

            return inventoryItem;
        }

        public InventoryItem RemoveItem(Consumable consumable)
        {
            if (!TryGetConsumable(consumable, out var inventoryItem))
                return null;

            Consumables.Remove(inventoryItem);

            return inventoryItem;
        }

        public InventoryItem RemoveItem(Material material, int count)
        {
            if (!TryGetMaterial(material, out var inventoryItem))
                return null;

            inventoryItem.Count.Value -= count;
            if (inventoryItem.Count.Value > 0)
                return null;


            Materials.Remove(inventoryItem);

            return inventoryItem;
        }

        public void RemoveItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (countEditableItem is null)
                    continue;

                RemoveItem(countEditableItem.ItemBase.Value, countEditableItem.Count.Value);
            }
        }

        public void RemoveItemsAll()
        {
            Consumables.DisposeAllAndClear();
            Equipments.DisposeAllAndClear();
            Materials.DisposeAllAndClear();
        }

        #endregion
        
        #region Try Get

        public bool TryGetItem(Guid guid, out ItemUsable itemUsable)
        {
            foreach (var inventoryItem in Equipments)
            {
                if (!(inventoryItem.ItemBase.Value is Equipment equipment)
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

            foreach (var item in Equipments)
            {
                if (!(item.ItemBase.Value is Equipment equipment))
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

        public bool TryGetConsumable(Consumable consumable, out InventoryItem inventoryItem)
        {
            if (consumable is null)
            {
                inventoryItem = null;
                return false;
            }

            foreach (var item in Consumables)
            {
                if (!(item.ItemBase.Value is Consumable food))
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
            foreach (var item in Materials)
            {
                if (item.ItemBase.Value.Data.Id != material.Data.Id)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        #endregion

        public void SelectItemView(InventoryItemView view)
        {
            if (view is null ||
                view.Model is null)
                return;
            
            SelectedItemView.Value = view;
            SelectedItemView.Value.Model.Selected.Value = true;
            SetGlowedAll(false);
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null ||
                SelectedItemView.Value.Model is null)
            {
                return;
            }

            SelectedItemView.Value.Model.Selected.Value = false;
            SelectedItemView.Value = null;
        }

        #region Subscribe

        private void SubscribeState(ItemType state)
        {
            DeselectItemView();
        }

        private void SubscribeDimmedFunc(Func<InventoryItem, bool> func)
        {
            if (DimmedFunc.Value == null)
            {
                DimmedFunc.Value = DefaultDimmedFunc;
            }

            foreach (var item in Equipments)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }
        }

        private void SubscribeItemOnClick(InventoryItemView view)
        {
            if (view != null &&
                view == SelectedItemView.Value)
            {
                DeselectItemView();

                return;
            }
        
            DeselectItemView();
            SelectItemView(view);
        }

        #endregion

        private static bool DefaultDimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in Equipments)
            {
                item.Glowed.Value = value;
            }

            foreach (var item in Consumables)
            {
                item.Glowed.Value = value;
            }

            foreach (var item in Materials)
            {
                item.Glowed.Value = value;
            }
        }
    }
}
