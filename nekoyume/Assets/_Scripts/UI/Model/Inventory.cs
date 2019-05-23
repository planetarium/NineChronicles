using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Item;
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
                var inventoryItem = new InventoryItem(item.Item, item.Count);
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

        public void Dispose()
        {
            items.DisposeAll();
            selectedItem.DisposeAll();
        }
        
        public InventoryItem AddItem(ItemBase addItemBase, int count)
        {
            var addedItem = items.FirstOrDefault(item => item.item.Value.Data.id == addItemBase.Data.id);
            if (ReferenceEquals(addedItem, null))
            {
                var result = new InventoryItem(addItemBase, count); 
                items.Add(result);
                return result;
            }
            
            addedItem.count.Value += count;
            return addedItem;
        }

        public void RemoveItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (ReferenceEquals(countEditableItem, null))
                {
                    continue;
                }

                RemoveItem(countEditableItem.item.Value.Data.id, countEditableItem.count.Value);
            }
        }

        public void RemoveItem(int id, int count)
        {
            var inventoryItem = items.FirstOrDefault(item => item.item.Value.Data.id == id);

            if (ReferenceEquals(inventoryItem, null))
            {
                return;
            }
            
            if (inventoryItem.count.Value > count)
            {
                inventoryItem.count.Value -= count;
            }
            else if (inventoryItem.count.Value == count)
            {
                items.Remove(inventoryItem);
            }
            else
            {
                throw new InvalidOperationException($"item({id}) count is lesser then {count}");
            }

            if (inventoryItem.count.Value > count)
            {
                inventoryItem.count.Value -= count;
            }
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

        public void SubscribeOnClick(InventoryItem inventoryItem)
        {
            if (!ReferenceEquals(selectedItem.Value, null))
            {
                selectedItem.Value.selected.Value = false;
                
                if (selectedItem.Value.item.Value.Data.id == inventoryItem.item.Value.Data.id)
                {
                    selectedItem.Value = null;
                    return;
                }
            }

            selectedItem.Value = inventoryItem;
            selectedItem.Value.selected.Value = true;

            foreach (var item in items)
            {
                item.glowed.Value = false;
            }
        }
        
        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private bool GlowedFunc(InventoryItem inventoryItem, Game.Item.ItemBase.ItemType type)
        {
            return false;
        }
    }
}
