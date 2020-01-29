using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Inventory : IState
    {
        // ToDo. Item 클래스를 FungibleItem과 NonFungibleItem으로 분리하기.
        [Serializable]
        public class Item : IState
        {
            public ItemBase item;
            public int count = 0;

            public Item(ItemBase itemBase, int count = 1)
            {
                item = ItemFactory.Create(itemBase.Data, default);
                this.count = count;
            }

            public Item(ItemUsable itemUsable, int count = 1)
            {
                item = itemUsable;
                this.count = count;
            }

            public Item(Bencodex.Types.Dictionary serialized)
            {
                item = ItemFactory.Deserialize(
                    (Bencodex.Types.Dictionary) serialized["item"]
                );
                count = (int) ((Integer) serialized["count"]).Value;
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

            public IValue Serialize()
            {
                return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "item"] = item.Serialize(),
                    [(Text) "count"] = (Integer) count,
                });
            }
        }

        private readonly List<Item> _items = new List<Item>();

        public IReadOnlyList<Item> Items => _items;

        public Inventory()
        {
        }

        public Inventory(Bencodex.Types.List serialized) : this()
        {
            _items.Capacity = serialized.Value.Length;
            foreach (IValue item in serialized)
            {
                _items.Add(new Item((Bencodex.Types.Dictionary) item));
            }
        }

        public KeyValuePair<int, int> AddItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase.Data.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    AddNonFungibleItem((ItemUsable) itemBase);
                    break;
                case ItemType.Material:
                    AddFungibleItem(itemBase, count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new KeyValuePair<int, int>(itemBase.Data.Id, count);
        }
        
        private Item AddFungibleItem(ItemBase itemBase, int count = 1)
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

        // Todo. NonFungibleItem 개발 후 `ItemBase itemBase` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        private Item AddNonFungibleItem(ItemUsable itemBase)
        {
            var nonFungibleItem = new Item(itemBase);
            _items.Add(nonFungibleItem);
            return nonFungibleItem;
        }

        public bool RemoveFungibleItem(ItemBase itemBase, int count = 1)
        {
            return itemBase is Material material && RemoveFungibleItem(material.Data.ItemId, count);
        }

        public bool RemoveFungibleItem(HashDigest<SHA256> id, int count = 1)
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
        
        public bool RemoveNonFungibleItem(Guid itemGuid)
        {
            return TryGetNonFungibleItem(itemGuid, out Item item) && _items.Remove(item);
        }

        // FungibleItem, NonFungibleItem 만들기
        public bool TryGetFungibleItem(ItemBase itemBase, out Item outFungibleItem)
        {
            if (itemBase is Material material)
                return TryGetFungibleItem(material.Data.ItemId, out outFungibleItem);

            outFungibleItem = null;
            return false;
        }

        public bool TryGetFungibleItem(HashDigest<SHA256> itemId, out Item outFungibleItem)
        {
            foreach (var fungibleItem in _items)
            {
                if (!(fungibleItem.item is Material material) || !material.Data.ItemId.Equals(itemId))
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
            return TryGetNonFungibleItem(itemUsable.ItemId, out outNonFungibleItem);
        }
        
        public bool TryGetNonFungibleItem(Guid itemGuid, out Item outNonFungibleItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is ItemUsable nonFungibleItem))
                {
                    continue;
                }

                if (nonFungibleItem.ItemId != itemGuid)
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
            outAddedItem = null;
            if (newItem is null)
                return false;
            try
            {
                outAddedItem = (ItemUsable) newItem.item;
            }
            catch (InvalidCastException)
            {
                var item = newItem.item;

                Debug.LogErrorFormat("Item {0}: {1} is not ItemUsable.", item.Data.ItemType, item.Data.Id);
            }
            return !(outAddedItem is null);
        }

        public bool HasItem(int id, int count = 1)
        {
            return _items.Exists(item => item.item.Data.Id == id && item.count >= count);
        }
        
        public bool HasItem(HashDigest<SHA256> id, int count = 1)
        {
            return _items.Exists(item =>
            {
                if (!(item.item is Material material))
                    return false;
                
                return material.Data.ItemId.Equals(id) && item.count >= count;
            });
        }

        public bool HasItem(Guid itemId) =>
            _items.Select(i => i.item).OfType<ItemUsable>().Any(i => i.ItemId == itemId);

        public bool TryGetNonFungibleItem(Guid itemId, out ItemUsable outNonFungibleItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is ItemUsable nonFungibleItem))
                {
                    continue;
                }

                if (nonFungibleItem.ItemId != itemId)
                {
                    continue;
                }

                outNonFungibleItem = nonFungibleItem;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }

        public IValue Serialize() =>
            new Bencodex.Types.List(Items.Select(i => i.Serialize()));
    }
}
