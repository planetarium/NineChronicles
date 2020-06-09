using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

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
                item = itemBase;
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
        
        public IEnumerable<Costume> Costumes => _items
            .Select(item => item.item)
            .OfType<Costume>();

        public IEnumerable<Equipment> Equipments => _items
            .Select(item => item.item)
            .OfType<Equipment>();

        public IEnumerable<Material> Materials => _items
            .Select(item => item.item)
            .OfType<Material>();

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

        public IValue Serialize() =>
            new Bencodex.Types.List(Items.Select(i => i.Serialize()));

        #region Add

        public KeyValuePair<int, int> AddItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    AddNonFungibleItem((ItemUsable) itemBase);
                    break;
                case ItemType.Costume:
                case ItemType.Material:
                    AddFungibleItem(itemBase, count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new KeyValuePair<int, int>(itemBase.Id, count);
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

        private Item AddNonFungibleItem(ItemUsable itemBase)
        {
            var nonFungibleItem = new Item(itemBase);
            _items.Add(nonFungibleItem);
            return nonFungibleItem;
        }

        #endregion

        #region Remove

        public bool RemoveFungibleItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase)
            {
                case Costume costume:
                    return RemoveCostume(costume.Id, count);
                case Material material:
                    return RemoveMaterial(material.ItemId, count);
                default:
                    return false;
            }
        }

        public bool RemoveCostume(int id, int count = 1)
        {
            if (!TryGetCostume(id, out var item) ||
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

        public bool RemoveMaterial(HashDigest<SHA256> id, int count = 1)
        {
            if (!TryGetMaterial(id, out var item) ||
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

        public bool RemoveNonFungibleItem(ItemUsable itemUsable)
        {
            return TryGetNonFungibleItem(itemUsable, out Item item) && _items.Remove(item);
        }

        public bool RemoveNonFungibleItem(Guid itemGuid)
        {
            return TryGetNonFungibleItem(itemGuid, out Item item) && _items.Remove(item);
        }

        #endregion

        #region Try Get

        public bool TryGetFungibleItem(ItemBase itemBase, out Item outFungibleItem)
        {
            switch (itemBase)
            {
                case Costume costume:
                    return TryGetCostume(costume.Id, out outFungibleItem);
                case Material material:
                    return TryGetMaterial(material.ItemId, out outFungibleItem);
                default:
                    outFungibleItem = null;
                    return false;
            }
        }

        public bool TryGetFungibleItem(int id, out Item outFungibleItem)
        {
            outFungibleItem = _items.FirstOrDefault(i => i.item.Id == id);
            return !(outFungibleItem is null);
        }

        public bool TryGetCostume(int id, out Item outCostume)
        {
            foreach (var item in _items)
            {
                if (!(item.item is Costume costume) ||
                    costume.Id != id)
                {
                    continue;
                }

                outCostume = item;
                return true;
            }

            outCostume = null;
            return false;
        }

        public bool TryGetMaterial(HashDigest<SHA256> itemId, out Item outMaterial)
        {
            foreach (var fungibleItem in _items)
            {
                if (!(fungibleItem.item is Material material) ||
                    !material.ItemId.Equals(itemId))
                {
                    continue;
                }

                outMaterial = fungibleItem;
                return true;
            }

            outMaterial = null;
            return false;
        }

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
            {
                return false;
            }

            try
            {
                outAddedItem = (ItemUsable) newItem.item;
            }
            catch (InvalidCastException)
            {
                var item = newItem.item;

                Log.Error("Item {0}: {1} is not ItemUsable.", item.ItemType, item.Id);
            }

            return !(outAddedItem is null);
        }

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

        #endregion

        #region Has

        public bool HasItem(int id, int count = 1)
        {
            return _items.Exists(item => item.item.Id == id && item.count >= count);
        }

        public bool HasItem(HashDigest<SHA256> id, int count = 1)
        {
            return _items.Exists(item =>
            {
                if (!(item.item is Material material))
                {
                    return false;
                }

                return material.ItemId.Equals(id) && item.count >= count;
            });
        }

        public bool HasItem(Guid itemId) =>
            _items.Select(i => i.item).OfType<ItemUsable>().Any(i => i.ItemId == itemId);

        #endregion
    }
}
