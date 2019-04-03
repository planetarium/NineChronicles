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
                a => a.Item.Data.Id.Equals(item.Data.Id)
                     && !item.Data.Cls.Contains("Weapon")
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

        }

        public void RemoveAt(int index)
        {

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
