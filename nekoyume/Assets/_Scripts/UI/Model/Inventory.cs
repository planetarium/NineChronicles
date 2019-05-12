using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveCollection<InventoryItem> items = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveProperty<InventoryItem> selectedItem = new ReactiveProperty<InventoryItem>(null);
        public readonly ReactiveProperty<Func<InventoryItem, bool>> dimmedFunc = new ReactiveProperty<Func<InventoryItem, bool>>();
        public readonly ReactiveProperty<Func<InventoryItem, Game.Item.ItemBase.ItemType, bool>> glowedFunc = new ReactiveProperty<Func<InventoryItem, Game.Item.ItemBase.ItemType, bool>>();

        public Inventory(List<Game.Item.Inventory.InventoryItem> items)
        {
            dimmedFunc.Value = DimmedFunc;
            glowedFunc.Value = GlowedFunc;
            
            items.ForEach(item =>
            {
                var inventoryItem = new InventoryItem(item);
                InitInventoryItem(inventoryItem);
                this.items.Add(inventoryItem);
            });

            this.items.ObserveAdd().Subscribe(added =>
            {
                InitInventoryItem(added.Value);
            });
            this.items.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            
            dimmedFunc.Subscribe(func =>
            {
                if (dimmedFunc.Value == null)
                {
                    dimmedFunc.Value = DimmedFunc;
                }
                
                foreach (var item in this.items)
                {
                    item.dimmed.Value = dimmedFunc.Value(item);
                }
            });
        }

        private static bool DimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private static bool GlowedFunc(InventoryItem inventoryItem, Game.Item.ItemBase.ItemType type)
        {
            return false;
        }

        public void Dispose()
        {
            items.DisposeAll();
            selectedItem.DisposeAll();
        }

        public void DeselectAll()
        {
            if (ReferenceEquals(selectedItem.Value, null))
            {
                return;
            }

            selectedItem.Value.selected.Value = false;
            selectedItem.Value = null;
        }

        private void InitInventoryItem(InventoryItem item)
        {
            item.dimmed.Value = dimmedFunc.Value(item);
            item.onClick.Subscribe(SubscribeOnClick);
        }

        private void SubscribeOnClick(InventoryItem inventoryItem)
        {
            if (!ReferenceEquals(selectedItem.Value, null))
            {
                selectedItem.Value.selected.Value = false;
                
                if (selectedItem.Value.item.Value.Item.Data.id == inventoryItem.item.Value.Item.Data.id)
                {
                    selectedItem.Value = null;
                    return;
                }
            }

            selectedItem.Value = inventoryItem;
            selectedItem.Value.selected.Value = true;

            foreach (var item in this.items)
            {
                item.glowed.Value = false;
            }
        }
    }
}
