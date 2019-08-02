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
                item = ItemBase.ItemFactory(itemBase.Data, default);
                this.count = count;
            }

            public Item(ItemUsable itemUsable, int count = 1)
            {
                item = itemUsable;
                this.count = count;
            }

            protected bool Equals(Item other)
            {
                return Equals(item, other.item) && count == other.count;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Item) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((item != null ? item.GetHashCode() : 0) * 397) ^ count;
                }
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

            var newFungibleItem = ItemBase.ItemFactory(itemRow, default);
            _items.Add(new Item(newFungibleItem, count));
        }

        // Todo. NonFungibleItem 개발 후 `ItemBase itemBase` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public Item AddNonFungibleItem(ItemUsable itemBase)
        {
            var nonFungibleItem = new Item(itemBase);
            _items.Add(nonFungibleItem);
            return nonFungibleItem;
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

                if (nonFungibleItem.ItemId != itemUsable.ItemId)
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
            foreach (var item in _items)
            {
                if (!(item.item is ItemUsable nonFungibleItem))
                {
                    continue;
                }

                if (nonFungibleItem.ItemId != itemUsable.ItemId)
                {
                    continue;
                }

                outNonFungibleItem = item;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }

        public bool TryGetAddedItemFrom(Inventory inventory, out ItemUsable outAddedItem)
        {
            //FIXME TryGetNonFungibleItem 내부에서 사용되는 아이템 비교방식때문에 오동작처리됨.
            //https://app.asana.com/0/958521740385861/1131813492738090/
            var newItem = _items.FirstOrDefault(i => !inventory.Items.Contains(i));
            outAddedItem = (ItemUsable) newItem?.item;
            return !(outAddedItem is null);
        }

        public bool HasItem(int id)
        {
            return _items.Exists(item => item.count > 0 && item.item.Data.id == id);
        }
    }
}
