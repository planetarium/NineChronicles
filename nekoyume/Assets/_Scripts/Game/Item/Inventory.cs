using System.Collections.Generic;


namespace Nekoyume.Game.Item
{
    public class Inventory
    {
        public class InventoryItem
        {
            public ItemBase Item;
            public int Count = 0;

            public InventoryItem(ItemBase item)
            {
                Item = ItemBase.ItemFactory(item.Data);
                Count += 1;
            }
        }

        public List<InventoryItem> _items;

        public Inventory()
        {
            _items = new List<InventoryItem> {Capacity = 40};
        }

        public bool Add(ItemBase item)
        {
            var i = _items.FindIndex(a => a.Item.Data.Id.Equals(item.Data.Id) && !item.Data.Cls.Contains("Weapon"));
            if (i < 0)
            {
                _items.Add(new InventoryItem(item));
            }
            else
            {
                _items[i].Count += 1;
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
            _items = items;
        }
    }
}
