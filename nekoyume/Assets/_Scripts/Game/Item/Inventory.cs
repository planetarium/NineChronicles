using System;
using System.Collections.Generic;
using Nekoyume.Data;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Inventory
    {
        [Serializable]
        public class Item
        {
            public ItemBase item;
            public int count = 0;
            
            public Item(ItemBase itemBase, int count = 1)
            {
                item = ItemBase.ItemFactory(itemBase.Data);
                this.count = count;
            }
            
            public Item(ItemUsable itemUsable, int count = 1)
            {
                item = itemUsable;
                this.count = count;
            }
        }

        public List<Item> items;

        public Inventory()
        {
            items = new List<Item> {Capacity = 40};
        }

        public Item Add(ItemBase item)
        {
            var i = items.FindIndex(
                a => a.item.Equals(item)
                     && !(item is Equipment)
            );
            if (i < 0)
            {
                var inventoryItem = new Item(item);
                items.Add(inventoryItem);
                return inventoryItem;
            }
            else
            {
                items[i].count += 1;
                return items[i];
            }
        }

        public void Remove(ItemBase item)
        {
            var i = items.FindIndex(ii => ii.item.Equals(item));
            RemoveAt(i);
        }

        public void RemoveAt(int index)
        {
            var inventoryItem = items[index];
            if (inventoryItem.count <= 1)
            {
                items.RemoveAt(index);
            }
            else
            {
                inventoryItem.count--;
            }
        }

        public ItemBase GetItem(int index)
        {
            return null;
        }

        public void Set(List<Item> items)
        {
            this.items = items;
        }
    }
}
