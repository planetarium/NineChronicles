using System;
using System.Collections.Generic;
using Nekoyume.Data;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Inventory
    {
        [Serializable]
        public class InventoryItem
        {
            public ItemBase Item;
            public int Count = 0;

            public InventoryItem(int id, int count = 1)
            {
                Item = Tables.instance.CreateItemBase(id);
                Count = count;
            }
            
            public InventoryItem(ItemBase item, int count = 1)
            {
                Item = ItemBase.ItemFactory(item.Data);
                Count = count;
            }

            public InventoryItem(InventoryItem item)
            {
                Item = item.Item;
                Count = item.Count;
            }
        }

        public List<InventoryItem> items;

        public Inventory()
        {
            items = new List<InventoryItem> {Capacity = 40};
        }

        public InventoryItem Add(ItemBase item)
        {
            var i = items.FindIndex(
                a => a.Item.Equals(item)
                     && !(item is Equipment)
            );
            if (i < 0)
            {
                var inventoryItem = new InventoryItem(item);
                items.Add(inventoryItem);
                return inventoryItem;
            }
            else
            {
                items[i].Count += 1;
                return items[i];
            }
        }

        public void Remove(ItemBase item)
        {
            var i = items.FindIndex(ii => ii.Item.Equals(item));
            RemoveAt(i);
        }

        public void RemoveAt(int index)
        {
            var inventoryItem = items[index];
            if (inventoryItem.Count <= 1)
            {
                items.RemoveAt(index);
            }
            else
            {
                inventoryItem.Count--;
            }
        }

        public ItemBase GetItem(int index)
        {
            return null;
        }

        public void Set(List<InventoryItem> items)
        {
            this.items = items;
        }
    }
}
