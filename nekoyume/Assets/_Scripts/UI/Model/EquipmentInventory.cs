using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;

namespace Nekoyume.UI.Model
{
    using UniRx;

    public class EquipmentInventory
    {
        public readonly ReactiveProperty<ItemSubType> State =
            new ReactiveProperty<ItemSubType>(ItemSubType.Weapon);

        public readonly Dictionary<ItemSubType, ReactiveCollection<InventoryItem>> Equipments =
            new Dictionary<ItemSubType, ReactiveCollection<InventoryItem>>()
            {
                {ItemSubType.Weapon, new ReactiveCollection<InventoryItem>()},
                {ItemSubType.Armor, new ReactiveCollection<InventoryItem>()},
                {ItemSubType.Belt, new ReactiveCollection<InventoryItem>()},
                {ItemSubType.Necklace, new ReactiveCollection<InventoryItem>()},
                {ItemSubType.Ring, new ReactiveCollection<InventoryItem>()}
            };

        public readonly ReactiveProperty<BigInventoryItemView> SelectedItemView =
            new ReactiveProperty<BigInventoryItemView>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> DimmedFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EffectEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> EquippedEnabledFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public readonly ReactiveProperty<Func<InventoryItem, bool>> ActiveFunc =
            new ReactiveProperty<Func<InventoryItem, bool>>();

        public EquipmentInventory()
        {
            State.Value = ItemSubType.Weapon;
            DimmedFunc.Value = DefaultDimmedFunc;

            State.Subscribe(x => { DeselectItemView(); });
            DimmedFunc.Subscribe(SetDimmedFunc);
            EffectEnabledFunc.Subscribe(SetEffectEnabledFunc);
            EquippedEnabledFunc.Subscribe(SetEquippedEnabledFunc);
            ActiveFunc.Subscribe(SetActiveFunc);
        }

        public void Dispose()
        {
            foreach (var equipment in Equipments)
            {
                equipment.Value.DisposeAllAndClear();
            }
        }

        public void ResetItems(Nekoyume.Model.Item.Inventory inventory)
        {
            DeselectItemView();
            Dispose();

            if (inventory is null)
            {
                return;
            }

            foreach (var item in inventory.Equipments)
            {
                AddItem(item);
            }

            State.SetValueAndForceNotify(State.Value);
        }

        public void AddItem(Equipment equipment)
        {
            if (equipment is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            var inventoryItem = CreateInventoryItem(equipment, 1);
            inventoryItem.EquippedEnabled.Value = equipment.equipped;
            Equipments[equipment.ItemSubType].Add(inventoryItem);
        }

        private InventoryItem CreateInventoryItem(ItemBase itemBase, int count)
        {
            var item = new InventoryItem(itemBase, count);
            item.Dimmed.Value = DimmedFunc.Value(item);

            return item;
        }

        public void SubscribeItemOnClick(BigInventoryItemView view)
        {
            SelectItemView(view);
        }

        private void SelectItemView(BigInventoryItemView view)
        {
            if (view?.Model is null)
            {
                return;
            }

            if (!view.Model.EffectEnabled.Value)
            {
                DeselectItemView();
            }

            SelectedItemView.SetValueAndForceNotify(view);
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
                if (!SelectedItemView.Value.Model.EffectEnabled.Value)
                {
                    SelectedItemView.Value.Model.Selected.Value = false;
                }
            }

            SelectedItemView.Value = null;
        }

        private void SetGlowedAll(bool value)
        {
            foreach (var item in Equipments.SelectMany(equipment => equipment.Value))
            {
                item.GlowEnabled.Value = value;
            }
        }

        public void UpdateDimAndEffectAll()
        {
            SetDimmedFunc(DimmedFunc.Value);
            SetEffectEnabledFunc(EffectEnabledFunc.Value);
        }

        private void SetDimmedFunc(Func<InventoryItem, bool> func)
        {
            if (DimmedFunc.Value == null)
            {
                DimmedFunc.SetValueAndForceNotify(DefaultDimmedFunc);
            }

            foreach (var item in Equipments.SelectMany(equipment => equipment.Value))
            {
                item.Dimmed.SetValueAndForceNotify(DimmedFunc.Value(item));
            }
        }

        private void SetEffectEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EffectEnabledFunc.Value == null)
            {
                EffectEnabledFunc.Value = DefaultCoveredFunc;
            }

            foreach (var item in Equipments.SelectMany(equipment => equipment.Value))
            {
                item.EffectEnabled.Value = EffectEnabledFunc.Value(item);
            }
        }

        private void SetEquippedEnabledFunc(Func<InventoryItem, bool> func)
        {
            if (EquippedEnabledFunc.Value == null)
            {
                EquippedEnabledFunc.Value = DefaultEquippedFunc;
            }

            foreach (var item in Equipments.SelectMany(equipment => equipment.Value))
            {
                item.EquippedEnabled.Value = EquippedEnabledFunc.Value(item);
            }
        }

        private void SetActiveFunc(Func<InventoryItem, bool> func)
        {
            ActiveFunc.Value ??= DefaultActiveFunc;

            foreach (var item in Equipments.SelectMany(equipment => equipment.Value))
            {
                item.ActiveSelf.Value = ActiveFunc.Value(item);
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
            if (inventoryItem.ItemBase.Value is Equipment equipment)
            {
                return equipment.equipped;
            }

            return false;
        }

        private static bool DefaultActiveFunc(InventoryItem inventoryItem)
        {
            return false;
        }
    }
}
