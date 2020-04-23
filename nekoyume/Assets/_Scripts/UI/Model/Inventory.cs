using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveProperty<ItemType> State =
            new ReactiveProperty<ItemType>(ItemType.Equipment);

        public readonly ReactiveCollection<InventoryItem> Consumables =
            new ReactiveCollection<InventoryItem>();

        public readonly ReactiveCollection<InventoryItem> Equipments =
            new ReactiveCollection<InventoryItem>();

        public readonly ReactiveCollection<InventoryItem> Materials =
            new ReactiveCollection<InventoryItem>();

        public readonly ReactiveProperty<InventoryItemView> SelectedItemView =
            new ReactiveProperty<InventoryItemView>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> DimmedFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EffectEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EquippedEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly Subject<InventoryItemView> OnDoubleClickItemView =
            new Subject<InventoryItemView>();

        public Inventory(ItemType stateType = ItemType.Equipment)
        {
            State.Value = stateType;
            DimmedFunc.Value = DefaultDimmedFunc;

            State.Subscribe(SubscribeState);
            DimmedFunc.Subscribe(SubscribeDimmedFunc);
            EffectEnabledFunc.Subscribe(SubscribeEffectEnabledFunc);
            EquippedEnabledFunc.Subscribe(SubscribeEquippedEnabledFunc);
        }

        public void Dispose()
        {
            State.Dispose();
            Consumables.Dispose();
            Equipments.Dispose();
            Materials.Dispose();
            SelectedItemView.Dispose();
            DimmedFunc.Dispose();
            EquippedEnabledFunc.Dispose();
            OnDoubleClickItemView.Dispose();
        }

        public void ResetItems(Nekoyume.Model.Item.Inventory inventory)
        {
            DeselectItemView();
            RemoveItemsAll();

            if (inventory is null)
            {
                return;
            }

            foreach (var item in inventory.Items)
            {
                AddItem(item.item, item.count);
            }

            State.SetValueAndForceNotify(State.Value);
        }

        #region Inventory Item Pool

        private InventoryItem CreateInventoryItem(ItemBase itemBase, int count)
        {
            var item = new InventoryItem(itemBase, count);
            item.Dimmed.Value = DimmedFunc.Value(item);
            item.OnClick.Subscribe(model =>
            {
                if (!(model is InventoryItem inventoryItem))
                {
                    return;
                }

                SubscribeItemOnClick(inventoryItem.View);
            });
            item.OnDoubleClick.Subscribe(model =>
            {
                if (!(model is InventoryItem inventoryItem))
                {
                    return;
                }

                DeselectItemView();
                OnDoubleClickItemView.OnNext(inventoryItem.View);
            });

            return item;
        }

        #endregion

        #region Add Item

        public void AddItem(ItemBase itemBase, int count = 1)
        {
            var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
            InventoryItem inventoryItem;
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                    var consumable = (Consumable) itemBase;
                    if (consumable.RequiredBlockIndex > blockIndex)
                    {
                        break;
                    }

                    inventoryItem = CreateInventoryItem(consumable, count);
                    Consumables.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment) itemBase;
                    if (equipment.RequiredBlockIndex > blockIndex)
                    {
                        break;
                    }

                    inventoryItem = CreateInventoryItem(equipment, count);
                    inventoryItem.EquippedEnabled.Value = equipment.equipped;
                    Equipments.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material) itemBase;
                    if (TryGetMaterial(material, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                        break;
                    }

                    inventoryItem = CreateInventoryItem(itemBase, count);
                    Materials.Add(inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Remove Item

        public void RemoveItem(ItemBase itemBase, int count = 1)
        {
            InventoryItem inventoryItem;
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                    if (!TryGetConsumable((ItemUsable) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    Consumables.Remove(inventoryItem);
                    break;
                case ItemType.Equipment:
                    if (!TryGetEquipment((ItemUsable) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    Equipments.Remove(inventoryItem);
                    break;
                case ItemType.Material:
                    if (!TryGetMaterial((Material) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    inventoryItem.Count.Value -= count;
                    if (inventoryItem.Count.Value > 0)
                    {
                        break;
                    }

                    Materials.Remove(inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RemoveItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (countEditableItem is null)
                {
                    continue;
                }

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

        public bool TryGetItem(ItemBase itemBase, out InventoryItem inventoryItem)
        {
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                    return TryGetConsumable((ItemUsable) itemBase, out inventoryItem);
                case ItemType.Equipment:
                    return TryGetEquipment((ItemUsable) itemBase, out inventoryItem);
                case ItemType.Material:
                    return TryGetMaterial((Material) itemBase, out inventoryItem);
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        public bool TryGetConsumable(ItemUsable itemUsable, out InventoryItem inventoryItem)
        {
            if (itemUsable is null)
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

                if (food.ItemId != itemUsable.ItemId)
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
            return TryGetMaterial(material.Data.ItemId, out inventoryItem);
        }

        public bool TryGetMaterial(HashDigest<SHA256> itemId, out InventoryItem inventoryItem)
        {
            foreach (var item in Materials)
            {
                if (!(item.ItemBase.Value is Material material) ||
                    !material.Data.ItemId.Equals(itemId))
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
            return false;
        }

        public bool TryGetMaterial(int id, out InventoryItem inventoryItem)
        {
            foreach (var item in Materials)
            {
                if (item.ItemBase.Value.Data.Id != id)
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

        private void SubscribeItemOnClick(InventoryItemView view)
        {
            if (view != null &&
                view == SelectedItemView.Value)
            {
                DeselectItemView();
                return;
            }

            SelectItemView(view);
        }

        public void SelectItemView(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }

            DeselectItemView();

            if (!(view.Model is null))
            {
                view.Model.Selected.Value = true;
            }

            SelectedItemView.Value = view;
            SetGlowedAll(false);
        }

        public void DeselectItemView()
        {
            if (SelectedItemView.Value is null)
            {
                return;
            }

            if (!(SelectedItemView.Value.Model is null))
            {
                SelectedItemView.Value.Model.Selected.Value = false;
            }

            SelectedItemView.Value = null;
        }

        public void UpdateDimAll()
        {
            SubscribeDimmedFunc(DimmedFunc.Value);
        }

        public void UpdateEffectAll()
        {
            SubscribeEffectEnabledFunc(EffectEnabledFunc.Value);
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

        private void SubscribeEffectEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EffectEnabledFunc.Value == null)
            {
                EffectEnabledFunc.Value = DefaultCoveredFunc;
            }

            foreach (var item in Equipments)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }
        }

        private void SubscribeEquippedEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EquippedEnabledFunc.Value == null)
            {
                EquippedEnabledFunc.Value = DefaultEquippedFunc;
            }

            foreach (var item in Equipments)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Consumables)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }
        }

        #endregion

        private static bool DefaultDimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private static bool DefaultCoveredFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private static bool DefaultEquippedFunc(InventoryItem inventoryItem)
        {
            if (!(inventoryItem.ItemBase.Value is Equipment equipment))
                return false;

            return equipment.equipped;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in Equipments)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Consumables)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Materials)
            {
                item.GlowEnabled.Value = value;
            }
        }
    }
}
