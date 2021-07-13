using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Model
{
    using UniRx;

    public class Inventory : IDisposable
    {
        public readonly ReactiveProperty<ItemType> State =
            new ReactiveProperty<ItemType>(ItemType.Equipment);

        public readonly ReactiveCollection<InventoryItem> Consumables =
            new ReactiveCollection<InventoryItem>();

        public readonly ReactiveCollection<InventoryItem> Costumes =
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

        public readonly ReactiveProperty<Func<InventoryItem, bool>> ActiveFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        private ItemSubType[] _itemSubTypesForNotification =
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring
        };

        public Inventory(ItemType stateType = ItemType.Equipment)
        {
            State.Value = stateType;
            DimmedFunc.Value = DefaultDimmedFunc;

            State.Subscribe(SubscribeState);
            DimmedFunc.Subscribe(SubscribeDimmedFunc);
            EffectEnabledFunc.Subscribe(SubscribeEffectEnabledFunc);
            EquippedEnabledFunc.Subscribe(SubscribeEquippedEnabledFunc);
            ActiveFunc.Subscribe(SubscribeAcitveFunc);
        }

        public void Dispose()
        {
            State.Dispose();
            Consumables.Dispose();
            Costumes.Dispose();
            Equipments.Dispose();
            Materials.Dispose();
            SelectedItemView.Dispose();
            DimmedFunc.Dispose();
            EquippedEnabledFunc.Dispose();
        }

        public void ResetItems(Nekoyume.Model.Item.Inventory inventory)
        {
            DeselectItemView();
            RemoveItemsAll();

            if (inventory is null)
            {
                return;
            }

            foreach (var item in inventory.Items.OrderByDescending(x => x.item is ITradableItem))
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

            return item;
        }

        private InventoryItem CreateInventoryItemTemp(ItemBase itemBase, int count)
        {
            var item = new InventoryItem(itemBase, count);
            item.Dimmed.Value = true;
            item.ForceDimmed = true;

            return item;
        }

        #endregion

        #region Add Item

        private void CreateTempItem(ItemBase itemBase, int count)
        {
            InventoryItem inventoryTempItem;
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    inventoryTempItem = CreateInventoryItemTemp(itemBase, count);
                    Consumables.Add(inventoryTempItem);
                    return;
                case ItemType.Costume:
                    var costume = (Costume) itemBase;
                    inventoryTempItem = CreateInventoryItemTemp(itemBase, count);
                    inventoryTempItem.EquippedEnabled.Value = costume.equipped;
                    Costumes.Add(inventoryTempItem);
                    return;
                case ItemType.Equipment:
                    var equipment = (Equipment) itemBase;
                    inventoryTempItem = CreateInventoryItemTemp(itemBase, count);
                    inventoryTempItem.EquippedEnabled.Value = equipment.equipped;
                    Equipments.Add(inventoryTempItem);
                    return;
                case ItemType.Material:
                    inventoryTempItem = CreateInventoryItemTemp(itemBase, count);
                    Materials.Add(inventoryTempItem);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AddItem(ItemBase itemBase, int count = 1)
        {
            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    if (Game.Game.instance.Agent.BlockIndex >
                        Game.Game.instance.TempExpiredBlockIndex)
                        return;

                    var agentAddress = Game.Game.instance.Agent.Address;
                    if (!Game.Game.instance.LegacyShopProducts.Products.ContainsKey(agentAddress))
                        return;

                    var shopItems = Game.Game.instance.LegacyShopProducts.Products[agentAddress];
                    foreach (var shopItem in shopItems)
                    {
                        if (shopItem.ItemUsable != null &&
                            shopItem.ItemUsable.ItemId == tradableItem.TradableId)
                        {
                            CreateTempItem(itemBase, count);
                            return;
                        }

                        if (shopItem.Costume != null &&
                            shopItem.Costume.ItemId == tradableItem.TradableId)
                        {
                            CreateTempItem(itemBase, count);
                            return;
                        }

                        if (shopItem.TradableFungibleItem != null &&
                            shopItem.TradableFungibleItem.TradableId == tradableItem.TradableId &&
                            shopItem.TradableFungibleItem.RequiredBlockIndex == tradableItem.RequiredBlockIndex)
                        {
                            CreateTempItem(itemBase, count);
                            return;
                        }
                    }
                    return;
                }
            }

            InventoryItem inventoryItem;
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    inventoryItem = CreateInventoryItem(itemBase, count);
                    Consumables.Add(inventoryItem);
                    break;
                case ItemType.Costume:
                    var costume = (Costume) itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count);
                    inventoryItem.EquippedEnabled.Value = costume.equipped;
                    Costumes.Add(inventoryItem);
                    break;
                case ItemType.Equipment:
                    var equipment = (Equipment) itemBase;
                    inventoryItem = CreateInventoryItem(itemBase, count);
                    inventoryItem.EquippedEnabled.Value = equipment.equipped;
                    Equipments.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material) itemBase;
                    bool istTradable = material is TradableMaterial;
                    if (TryGetMaterial(material, istTradable, out inventoryItem))
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
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    if (!TryGetConsumable((ItemUsable) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    Consumables.Remove(inventoryItem);
                    break;
                case ItemType.Costume:
                    if (!TryGetCostume((Costume) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    inventoryItem.Count.Value -= count;
                    if (inventoryItem.Count.Value > 0)
                    {
                        break;
                    }

                    Costumes.Remove(inventoryItem);
                    break;
                case ItemType.Equipment:
                    if (!TryGetEquipment((ItemUsable) itemBase, out inventoryItem))
                    {
                        break;
                    }

                    Equipments.Remove(inventoryItem);
                    break;
                case ItemType.Material:
                    bool isTradable = itemBase is TradableMaterial;
                    if (!TryGetMaterial((Material) itemBase, isTradable, out inventoryItem))
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
            Costumes.DisposeAllAndClear();
            Equipments.DisposeAllAndClear();
            Materials.DisposeAllAndClear();
        }

        #endregion

        #region Try Get

        public bool TryGetItem(ItemBase itemBase, out InventoryItem inventoryItem)
        {
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                    return TryGetConsumable((ItemUsable) itemBase, out inventoryItem);
                case ItemType.Costume:
                    return TryGetCostume((ItemUsable) itemBase, out inventoryItem);
                case ItemType.Equipment:
                    return TryGetEquipment((ItemUsable) itemBase, out inventoryItem);
                case ItemType.Material:
                    bool isTradable = itemBase is TradableMaterial;
                    return TryGetMaterial((Material) itemBase, isTradable, out inventoryItem);
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        public bool TryGetCostume(ItemUsable itemUsable, out InventoryItem inventoryItem)
        {
            if (itemUsable is null)
            {
                inventoryItem = null;
                return false;
            }

            return TryGetCostume(itemUsable.Id, out inventoryItem);
        }

        public bool TryGetCostume(Costume costume, out InventoryItem inventoryItem)
        {
            if (costume is null)
            {
                inventoryItem = null;
                return false;
            }

            return TryGetCostume(costume.Id, out inventoryItem);
        }

        public bool TryGetCostume(int id, out InventoryItem inventoryItem)
        {
            foreach (var item in Costumes)
            {
                if (item.ItemBase.Value.Id != id)
                {
                    continue;
                }

                inventoryItem = item;
                return true;
            }

            inventoryItem = null;
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

        public bool TryGetMaterial(Material material, bool isTradableMaterial, out InventoryItem inventoryItem)
        {
            if (material is null)
            {
                inventoryItem = null;
                return false;
            }

            return TryGetMaterial(material.ItemId, isTradableMaterial, out inventoryItem);
        }

        public bool TryGetMaterial(HashDigest<SHA256> itemId, bool isTradableMaterial, out InventoryItem inventoryItem)
        {
            foreach (var item in Materials)
            {
                if (item.ForceDimmed)
                {
                    continue;
                }

                if (!(item.ItemBase.Value is Material material) ||
                    !material.ItemId.Equals(itemId))
                {
                    continue;
                }

                if (isTradableMaterial)
                {
                    if (!(item.ItemBase.Value is TradableMaterial tradableMaterial))
                    {
                        continue;
                    }
                }
                else
                {
                    if ((item.ItemBase.Value is TradableMaterial tradableMaterial))
                    {
                        continue;
                    }
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
                if (item.ItemBase.Value.Id != id)
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

        public void SubscribeItemOnClick(InventoryItemView view)
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
            if (view is null ||
                view.Model is null)
            {
                return;
            }

            DeselectItemView();

            view.Model.Selected.Value = true;
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
            UpdateEquipmentNotification();
        }

        private void SubscribeDimmedFunc(Func<InventoryItem, bool> func)
        {
            if (DimmedFunc.Value == null)
            {
                DimmedFunc.Value = DefaultDimmedFunc;
            }

            foreach (var item in Consumables)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Costumes)
            {
                item.Dimmed.Value = DimmedFunc.Value(item);
            }

            foreach (var item in Equipments)
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

            foreach (var item in Consumables)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Costumes)
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }

            foreach (var item in Equipments)
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

            foreach (var item in Consumables)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Costumes)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Equipments)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }
        }

        private void SubscribeAcitveFunc(Func<InventoryItem, bool> func)
        {
            ActiveFunc.Value ??= DefaultAcitveFunc;

            foreach (var item in Consumables)
            {
                item.ActiveSelf.Value = ActiveFunc.Value(item);
            }

            foreach (var item in Costumes)
            {
                item.ActiveSelf.Value = ActiveFunc.Value(item);
            }

            foreach (var item in Equipments)
            {
                item.ActiveSelf.Value = ActiveFunc.Value(item);
            }

            foreach (var item in Materials)
            {
                item.ActiveSelf.Value = ActiveFunc.Value(item);
            }
        }
        #endregion

        public void UpdateEquipmentNotification(List<ElementalType> elementalTypes = null)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
                return;

            if (State.Value != ItemType.Equipment)
                return;

            var equipments = Equipments;

            foreach (var item in equipments)
            {
                item.HasNotification.Value = false;
            }

            var level = currentAvatarState.level;
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level);

            foreach (var (type, slotCount) in availableSlots)
            {
                var matchedEquipments = Equipments
                    .Where(e => e.ItemBase.Value.ItemSubType == type);

                if (elementalTypes != null)
                {
                    matchedEquipments = matchedEquipments.Where(e =>
                        elementalTypes.Exists(x => x == e.ItemBase.Value.ElementalType));
                }
                var equippedEquipments =
                    matchedEquipments.Where(e => e.EquippedEnabled.Value);
                var unequippedEquipments =
                    matchedEquipments.Where(e => !e.EquippedEnabled.Value)
                    .OrderByDescending(i => CPHelper.GetCP(i.ItemBase.Value as Equipment));

                var equippedCount = equippedEquipments.Count();

                if (equippedCount < slotCount)
                {
                    var itemsToNotify = unequippedEquipments.Take(slotCount - equippedCount);

                    foreach (var item in itemsToNotify)
                    {
                        item.HasNotification.Value = true;
                    }
                }
                else
                {
                    var itemsToNotify =
                        unequippedEquipments.Where(e =>
                        {
                            var cp = CPHelper.GetCP(e.ItemBase.Value as Equipment);
                            return equippedEquipments.Any(i => CPHelper.GetCP(i.ItemBase.Value as Equipment) < cp);
                        }).Take(slotCount);
                    foreach (var item in itemsToNotify)
                    {
                        item.HasNotification.Value = true;
                    }
                }
            }
        }

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
            switch (inventoryItem.ItemBase.Value)
            {
                case Costume costume:
                    return costume.equipped;
                case Equipment equipment:
                    return equipment.equipped;
                default:
                    return false;
            }
        }

        private static bool DefaultAcitveFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in Consumables)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Costumes)
            {
                item.GlowEnabled.Value = value;
            }

            foreach (var item in Equipments)
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
