using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Inventory
    {
        // ToDo. Item 클래스를 FungibleItem과 NonFungibleItem으로 분리하기.
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

        // Todo. NonFungibleItem 개발 후 `ItemBase itemBase` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public Item AddNonFungibleItem(ItemUsable itemBase)
        {
            var nonFungibleItem = new Item(itemBase);
            _items.Add(nonFungibleItem);
            return nonFungibleItem;
        }

        // Todo. NonFungibleItem 개발 후 `int id` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public void AddNonFungibleItem(int id)
        {
            if (!Tables.instance.TryGetItemEquipment(id, out var itemEquipmentRow))
            {
                throw new KeyNotFoundException($"itemId: {id}");
            }

            var nonFungibleItem = ItemBase.ItemFactory(itemEquipmentRow);
            _items.Add(new Item(nonFungibleItem));
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

        // Todo. NonFungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public bool RemoveNonFungibleItem(ItemUsable itemUsable)
        {
            return TryGetNonFungibleItem(itemUsable, out Item item) && _items.Remove(item);
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

        // Todo. NonFungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public bool TryGetNonFungibleItem(ItemUsable itemUsable, out ItemUsable outNonFungibleItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is ItemUsable nonFungibleItem))
                {
                    continue;
                }

                if (nonFungibleItem.Data.id != itemUsable.Data.id ||
                    !nonFungibleItem.Stats.Equals(itemUsable.Stats))
                {
                    continue;
                }
                
                outNonFungibleItem = nonFungibleItem;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }
        
        public bool TryGetNonFungibleItemFromLast(out ItemUsable outNonFungibleItem)
        {
            foreach (var item in Enumerable.Reverse(_items))
            {
                if (!(item.item is ItemUsable nonFungibleItem))
                {
                    continue;
                }

                outNonFungibleItem = nonFungibleItem;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }

        public bool TryGetNonFungibleItem(ItemUsable itemUsable, out Item outNonFungibleItem)
        {
            foreach (var nonFungibleItem in _items)
            {
                if (nonFungibleItem.item.Data.id != itemUsable.Data.id)
                {
                    continue;
                }

                outNonFungibleItem = nonFungibleItem;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }

        public bool TryGetAddedItemFrom(Inventory inventory, out ItemUsable outAddedItem)
        {
            // 인벤토리에서 추가된 아이템 확인.
            foreach (var item in _items)
            {
                // 장비나 소모품이 아닌가?
                if (!(item.item is ItemUsable itemUsable))
                {
                    continue;
                }

                // 원래 갖고 있었나?
                if (inventory.TryGetNonFungibleItem(itemUsable, out ItemUsable outUnfungibleItem))
                {
                    continue;
                }

                outAddedItem = itemUsable;
                return true;
            }

            outAddedItem = null;
            return false;
        }
    }
}
