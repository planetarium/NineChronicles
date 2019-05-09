using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryAndItemInfo : IDisposable
    {
        public readonly ReactiveProperty<Inventory> inventory = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ItemInfo> itemInfo = new ReactiveProperty<ItemInfo>();

        public InventoryAndItemInfo(List<Game.Item.Inventory.InventoryItem> items)
        {
            inventory.Value = new Inventory(items);
            itemInfo.Value = new ItemInfo();
            
            inventory.Value.selectedItem.Subscribe(OnInventorySelectedItem);
        }

        public void Dispose()
        {
            inventory.DisposeAll();
            itemInfo.DisposeAll();
        }
        
        public void AddToInventory(CountableItem item)
        {
            using (var e = inventory.Value.items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null) ||
                        e.Current.item.Value.Item.Data.id != item.item.Value.Item.Data.id)
                    {
                        continue;
                    }

                    e.Current.count.Value += item.count.Value;
                    return;
                }
            }
            
            inventory.Value.items.Add(new InventoryItem(item.item.Value, item.count.Value));
        }

        public void RemoveFromInventory(ICollection<CountEditableItem> items)
        {
            var shouldRemoveItems = new List<CountEditableItem>();
            
            using (var e = items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null))
                    {
                        continue;
                    }

                    var stagedItem = e.Current;
                    
                    using (var e2 = inventory.Value.items.GetEnumerator())
                    {
                        while (e2.MoveNext())
                        {
                            if (ReferenceEquals(e2.Current, null) ||
                                e2.Current.item.Value.Item.Data.id != stagedItem.item.Value.Item.Data.id)
                            {
                                continue;
                            }

                            var inventoryItem = e2.Current;
                            inventoryItem.count.Value -= stagedItem.count.Value;

                            if (inventoryItem.count.Value == 0)
                            {
                                inventory.Value.items.Remove(inventoryItem);
                            }

                            shouldRemoveItems.Add(stagedItem);
                            
                            break;
                        }
                    }
                }
            }
            
            shouldRemoveItems.ForEach(item => items.Remove(item));
        }
        
        private void OnInventorySelectedItem(InventoryItem data)
        {
            itemInfo.Value.item.Value = data;
        }
    }
}
