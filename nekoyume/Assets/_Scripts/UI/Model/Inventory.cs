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
        
        public void AddToInventory(CountableItem item)
        {
            using (var e = items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null) ||
                        e.Current.item.Value.Data.id != item.item.Value.Data.id)
                    {
                        continue;
                    }

                    e.Current.count.Value += item.count.Value;
                    return;
                }
            }
            
            items.Add(new InventoryItem(item.item.Value, item.count.Value));
        }

        public void RemoveFromInventory(IEnumerable<CountEditableItem> collection)
        {
            var shouldRemoveItems = new List<InventoryItem>();
            
            using (var e = collection.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null))
                    {
                        continue;
                    }

                    var removeItem = e.Current;
                    
                    using (var e2 = items.GetEnumerator())
                    {
                        while (e2.MoveNext())
                        {
                            var inventoryItem = e2.Current;
                            
                            if (ReferenceEquals(inventoryItem, null) ||
                                inventoryItem.item.Value.Data.id != removeItem.item.Value.Data.id)
                            {
                                continue;
                            }

                            inventoryItem.count.Value -= removeItem.count.Value;

                            if (inventoryItem.count.Value <= 0)
                            {
                                shouldRemoveItems.Add(inventoryItem);
                            }
                            
                            break;
                        }
                    }
                }
            }
            
            shouldRemoveItems.ForEach(item => items.Remove(item));
        }

        public void RemoveItem(int id, int count)
        {
            InventoryItem shouldRemove = null;
            foreach (var item in items)
            {
                if (item.item.Value.Data.id != id)
                {
                    continue;
                }
                
                if (item.count.Value > count)
                {
                    item.count.Value -= count;
                }
                else if (item.count.Value == count)
                {
                    shouldRemove = item;
                }
                else
                {
                    throw new InvalidOperationException($"item({id}) count is lesser then {count}");
                }
                
                break;
            }

            if (!ReferenceEquals(shouldRemove, null))
            {
                items.Remove(shouldRemove);
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

        private void SubscribeOnClick(InventoryItem inventoryItem)
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
