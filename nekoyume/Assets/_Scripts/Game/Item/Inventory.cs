using System;
using System.Collections.Generic;
using Nekoyume.Data;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Inventory
    {
        // ToDo. Item 클래스를 FungibleItem과 UnfungibleItem으로 분리하기.
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

        private readonly List<Item> _items = new List<Item>();

        public IEnumerable<Item> Items => _items;
        
        public Item AddFungibleItem(ItemBase itemBase, int count = 1)
        {
            if (TryGetFungibleItem(itemBase, out var fungibleItem))
            {
                fungibleItem.count += count;
                return fungibleItem;
            }
            
            fungibleItem = new Item(itemBase, count);
            _items.Add(fungibleItem);
            return fungibleItem;
        }
        
        public void AddFungibleItem(int id, int count = 1)
        {
            if (TryGetFungibleItem(id, out var fungibleItem))
            {
                fungibleItem.count += count;
                
                return;
            }
            
            if (!Tables.instance.TryGetItem(id, out var itemRow))
            {
                throw new KeyNotFoundException($"itemId: {id}");
            }

            var newFungibleItem = ItemBase.ItemFactory(itemRow);
            _items.Add(new Item(newFungibleItem, count));
        }
        
        // Todo. UnfungibleItem 개발 후 `ItemBase itemBase` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public Item AddUnfungibleItem(ItemUsable itemBase)
        {
            var unfungibleItem = new Item(itemBase);
            _items.Add(unfungibleItem);
            return unfungibleItem;
        }
        
        // Todo. UnfungibleItem 개발 후 `int id` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public void AddUnfungibleItem(int id)
        {
            if (!Tables.instance.TryGetItemEquipment(id, out var itemEquipmentRow))
            {
                throw new KeyNotFoundException($"itemId: {id}");
            }

            var unfungibleItem = ItemBase.ItemFactory(itemEquipmentRow);
            _items.Add(new Item(unfungibleItem));
        }

        public bool RemoveFungibleItem(ItemBase itemBase, int count = 1)
        {
            return RemoveFungibleItem(itemBase.Data.id, count);
        }
        
        public bool RemoveFungibleItem(int id, int count = 1)
        {
            if (!TryGetFungibleItem(id, out var item) ||
                item.count < count)
            {
                return false;
            }
            
            item.count -= count;
            if (item.count == 0)
            {
                _items.Remove(item);
            }

            return true;
        }
        
        // Todo. UnfungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public bool RemoveUnfungibleItem(ItemUsable itemUsable)
        {
            return TryGetUnfungibleItem(itemUsable, out Item item) && _items.Remove(item);
        }
        
        public bool TryGetFungibleItem(ItemBase itemBase, out Item outFungibleItem)
        {
            return TryGetFungibleItem(itemBase.Data.id, out outFungibleItem);
        }
        
        public bool TryGetFungibleItem(int id, out Item outFungibleItem)
        {
            foreach (var fungibleItem in _items)
            {
                if (fungibleItem.item.Data.id != id)
                {
                    continue;
                }
                
                outFungibleItem = fungibleItem;
                return true;
            }

            outFungibleItem = null;
            return false;
        }

        // Todo. UnfungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `UnfungibleItem unfungibleItem`로 수정.
        public bool TryGetUnfungibleItem(ItemUsable itemUsable, out ItemUsable outUnfungibleItem)
        {
            foreach (var unfungibleItem in _items)
            {
                if (unfungibleItem.item.Data.id != itemUsable.Data.id)
                {
                    continue;
                }
                
                outUnfungibleItem = (ItemUsable) unfungibleItem.item;
                return true;
            }

            outUnfungibleItem = null;
            return false;
        }
        
        public bool TryGetUnfungibleItem(ItemUsable itemUsable, out Item outUnfungibleItem)
        {
            foreach (var unfungibleItem in _items)
            {
                if (unfungibleItem.item.Data.id != itemUsable.Data.id)
                {
                    continue;
                }
                
                outUnfungibleItem = unfungibleItem;
                return true;
            }

            outUnfungibleItem = null;
            return false;
        }
    }
}
