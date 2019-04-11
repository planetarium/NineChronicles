using System;
using System.Collections.Generic;

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

        public bool Add(ItemBase item)
        {
            var i = items.FindIndex(
                a => a.Item.Equals(item)
                     && !(item is Equipment)
                     && !item.reserved
            );
            if (i < 0)
            {
                items.Add(new InventoryItem(item));
            }
            else
            {
                items[i].Count += 1;
            }
            return true;
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
