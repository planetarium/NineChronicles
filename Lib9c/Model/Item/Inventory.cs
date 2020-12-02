using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Inventory : IState
    {
        // ToDo. Item 클래스를 FungibleItem과 NonFungibleItem으로 분리하기.
        [Serializable]
        // FIXME 구현해야 합니다.
#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        public class Item : IState, IComparer<Item>, IComparable<Item>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        {
            public ItemBase item;
            public int count = 0;

            public Item(ItemBase itemBase, int count = 1)
            {
                item = itemBase;
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

            public int Compare(Item x, Item y)
            {
                return x.item.Grade != y.item.Grade
                    ? y.item.Grade.CompareTo(x.item.Grade)
                    : x.item.Id.CompareTo(y.item.Id);
            }

            public int CompareTo(Item other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return Compare(this, other);
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
        
        public IEnumerable<Consumable> Consumables => _items
            .Select(item => item.item)
            .OfType<Consumable>();

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
            _items.Sort();
        }

        public IValue Serialize() => new Bencodex.Types.List(Items
            .OrderBy(i => i.item.Id)
            .ThenByDescending(i => i.count)
            .Select(i => i.Serialize()));

        #region Add

        public KeyValuePair<int, int> AddItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                case ItemType.Costume:
                    AddNonFungibleItem(itemBase);
                    break;
                case ItemType.Material:
                    AddFungibleItem(itemBase, count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _items.Sort();
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

        private Item AddNonFungibleItem(ItemBase itemBase)
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
                case Material material:
                    return RemoveMaterial(material.ItemId, count);
                default:
                    return false;
            }
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

        public bool RemoveNonFungibleItem(INonFungibleItem nonFungibleItem)
        {
            return RemoveNonFungibleItem(nonFungibleItem.ItemId);
        }

        public bool RemoveNonFungibleItem(Guid itemId)
        {
            return TryGetNonFungibleItem(itemId, out Item item) && _items.Remove(item);
        }

        public bool LegacyRemoveNonFungibleItem(Costume costume)
        {
            return LegacyRemoveNonFungibleItem(costume.ItemId);
        }

        public bool LegacyRemoveNonFungibleItem(Guid itemId)
        {
            var isRemoved = TryGetNonFungibleItem(itemId, out Item item);
            if (!isRemoved) return false;

            foreach (var element in _items)
            {
                if (element.item.Id == item.item.Id)
                {
                    _items.Remove(element);
                    break;
                }
            }
            return true;            
        }

        #endregion

        #region Try Get

        public bool TryGetFungibleItem(ItemBase itemBase, out Item outFungibleItem)
        {
            switch (itemBase)
            {
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

        // FIXME: It must be deleted. As ItemId was added to the costume, it became NonFungible.
        public bool TryGetCostume(int id, out Costume outCostume)
        {
            foreach (var item in _items)
            {
                if (!(item.item is Costume costume) ||
                    !costume.Id.Equals(id))
                {
                    continue;
                }

                outCostume = costume;
                return true;
            }

            outCostume = null;
            return false;
        }

        public bool TryGetNonFungibleItem(Guid itemId, out Item outInventoryItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is INonFungibleItem nonFungibleItem) ||
                    !nonFungibleItem.ItemId.Equals(itemId))
                {
                    continue;
                }

                outInventoryItem = item;
                return true;
            }

            outInventoryItem = null;
            return false;
        }

        public bool TryGetNonFungibleItem<T>(T nonFungibleItem, out T outNonFungibleItem) where T : INonFungibleItem
        {
            return TryGetNonFungibleItem(nonFungibleItem.ItemId, out outNonFungibleItem);
        }

        public bool TryGetNonFungibleItem<T>(Guid itemId, out T outNonFungibleItem) where T : INonFungibleItem
        {
            foreach (var item in _items)
            {
                if (!(item.item is T nonFungibleItem))
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

            outNonFungibleItem = default;
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

        public bool HasNotification(int level)
        {
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level);

            foreach (var (type, slotCount) in availableSlots)
            {
                var equipments = Equipments.Where(e => e.ItemSubType == type);
                var current = equipments.Where(e => e.equipped);
                // When an equipment slot is empty.
                if (current.Count() < Math.Min(equipments.Count(), slotCount))
                {
                    return true;
                }

                // When any other equipments are stronger than current one.
                foreach (var equipment in equipments)
                {
                    if (equipment.equipped)
                        continue;

                    var cp = CPHelper.GetCP(equipment);
                    if (current.Any(i => CPHelper.GetCP(i) < cp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
