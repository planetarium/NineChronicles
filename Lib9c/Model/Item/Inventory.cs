using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.State;

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

        protected bool Equals(Inventory other)
        {
            if (_items.Count == 0 && other._items.Count == 0)
            {
                return true;
            }

            return Equals(_items, other._items);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Inventory) obj);
        }

        public override int GetHashCode()
        {
            return (_items != null ? _items.GetHashCode() : 0);
        }

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

        public Item AddFungibleItem(ItemBase itemBase, int count = 1)
        {
            switch (itemBase)
            {
                case Material material:
                    return AddMaterial(material, count);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Item AddMaterial(Material material, int count = 1)
        {
            if (TryGetFungibleItems(material.FungibleId, out var items))
            {
                var ownedItem = items.FirstOrDefault(e =>
                    e.item is Material ownedMaterial &&
                    ownedMaterial.IsTradable == material.IsTradable);
                if (!(ownedItem is null))
                {
                    ownedItem.count += count;
                    return ownedItem;
                }
            }

            var newItem = new Item(material, count);
            _items.Add(newItem);
            return newItem;
        }

        public Item AddNonFungibleItem(ItemBase itemBase)
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

        public bool RemoveTradableItem(ITradableItem tradableItem, int count = 1)
        {
            switch (tradableItem)
            {
                case INonFungibleItem nonFungibleItem:
                    return RemoveNonFungibleItem(nonFungibleItem);
                case Material material:
                    return RemoveTradableMaterial(material.ItemId, count);
                default:
                    return false;
            }
        }

        public bool RemoveMaterial(HashDigest<SHA256> fungibleId, int count = 1)
        {
            if (!TryGetFungibleItems(fungibleId, out var items) ||
                items.Sum(item => item.count) < count)
            {
                return false;
            }

            var nonTradableMaterial =
                items.FirstOrDefault(item => !((Material) item.item).IsTradable);
            if (nonTradableMaterial != null)
            {
                if (nonTradableMaterial.count > count)
                {
                    nonTradableMaterial.count -= count;
                    return true;
                }

                count -= nonTradableMaterial.count;
                _items.Remove(nonTradableMaterial);
                if (count == 0)
                {
                    return true;
                }
            }

            var tradableMaterial = items.FirstOrDefault(item => ((Material) item.item).IsTradable);
            if (tradableMaterial != null)
            {
                tradableMaterial.count -= count;
                if (tradableMaterial.count == 0)
                {
                    _items.Remove(tradableMaterial);
                }
            }

            return true;
        }

        public bool RemoveTradableMaterial(HashDigest<SHA256> fungibleId, int count = 1)
        {
            var tradeId = Material.DeriveTradableId(fungibleId);
            if (!TryGetTradableItems(tradeId, out var items))
            {
                return false;
            }

            var item = items.FirstOrDefault(e =>
                e.item is Material material &&
                material.IsTradable);
            if (item is null)
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
            return RemoveNonFungibleItem(nonFungibleItem.NonFungibleId);
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

        // FIXME: It must be deleted. As ItemId was added to the costume, it became INonFungibleItem.
        [Obsolete("Use public bool TryGetNonFungibleItem<T>(Guid itemId, out T outNonFungibleItem)")]
        public bool TryGetCostume(int rowId, out Costume outCostume)
        {
            foreach (var item in _items)
            {
                if (!(item.item is Costume costume) ||
                    !costume.Id.Equals(rowId))
                {
                    continue;
                }

                outCostume = costume;
                return true;
            }

            outCostume = null;
            return false;
        }

        public bool TryGetItem(int rowId, out Item outItem)
        {
            outItem = _items.FirstOrDefault(e => e.item.Id == rowId);
            return !(outItem is null);
        }

        public bool TryGetFungibleItems(HashDigest<SHA256> fungibleId, out List<Item> outItems)
        {
            outItems = new List<Item>();
            foreach (var item in _items)
            {
                if (item.item is IFungibleItem fungibleItem &&
                    fungibleItem.FungibleId.Equals(fungibleId))
                {
                    outItems.Add(item);
                }
            }

            return outItems.Count > 0;
        }

        public bool TryGetNonFungibleItem(Guid itemId, out Item outItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is INonFungibleItem nonFungibleItem) ||
                    !nonFungibleItem.NonFungibleId.Equals(itemId))
                {
                    continue;
                }

                outItem = item;
                return true;
            }

            outItem = null;
            return false;
        }

        public bool TryGetNonFungibleItem<T>(T nonFungibleItem, out T outNonFungibleItem)
            where T : INonFungibleItem =>
            TryGetNonFungibleItem(nonFungibleItem.NonFungibleId, out outNonFungibleItem);

        public bool TryGetNonFungibleItem<T>(Guid itemId, out T outNonFungibleItem)
            where T : INonFungibleItem
        {
            foreach (var item in _items)
            {
                if (!(item.item is T nonFungibleItem) ||
                    !nonFungibleItem.NonFungibleId.Equals(itemId))
                {
                    continue;
                }

                outNonFungibleItem = nonFungibleItem;
                return true;
            }

            outNonFungibleItem = default;
            return false;
        }

        public bool TryGetTradableItems(Guid tradeId, out List<Item> outItems)
        {
            outItems = new List<Item>();
            foreach (var item in _items)
            {
                if (!(item.item is ITradableItem tradableItem) ||
                    !tradableItem.TradableId.Equals(tradeId))
                {
                    continue;
                }

                outItems.Add(item);
            }

            return outItems.Any();
        }

        public bool TryGetTradableItemWithoutNonTradableFungibleItem(
            Guid tradeId,
            out Item outItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is ITradableItem tradableItem) ||
                    !tradableItem.TradableId.Equals(tradeId))
                {
                    continue;
                }

                if (tradableItem is IFungibleItem fungibleItem &&
                    !fungibleItem.IsTradable)
                {
                    continue;
                }

                outItem = item;
                return true;
            }

            outItem = null;
            return false;
        }

        public bool TryGetNonTradableFungibleItem(
            HashDigest<SHA256> fungibleId,
            out Item outItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is IFungibleItem fungibleItem) ||
                    fungibleItem.IsTradable ||
                    !fungibleItem.FungibleId.Equals(fungibleId))
                {
                    continue;
                }

                outItem = item;
                return true;
            }

            outItem = null;
            return false;
        }

        #endregion

        #region Has

        public bool HasItem(int id, int count = 1)
        {
            return _items.Exists(item => item.item.Id == id && item.count >= count);
        }

        public bool HasItem(HashDigest<SHA256> itemId, int count = 1)
        {
            return _items.Exists(item =>
            {
                if (!(item.item is Material material))
                {
                    return false;
                }

                return material.ItemId.Equals(itemId) && item.count >= count;
            });
        }

        public bool HasItem(Guid itemId) => _items
            .Select(i => i.item)
            .OfType<INonFungibleItem>()
            .Any(i => i.NonFungibleId.Equals(itemId));

        public bool HasTradableItem(Guid tradeId) => _items
            .Select(i => i.item)
            .OfType<ITradableItem>()
            .Any(i => i.TradableId.Equals(tradeId));

        #endregion

        public bool HasNotification(int level, long blockIndex)
        {
            var availableSlots = UnlockHelper.GetAvailableEquipmentSlots(level);

            foreach (var (type, slotCount) in availableSlots)
            {
                var equipments = Equipments
                    .Where(e =>
                        e.ItemSubType == type &&
                        e.RequiredBlockIndex <= blockIndex)
                    .ToList();
                var current = equipments.Where(e => e.equipped).ToList();
                // When an equipment slot is empty.
                if (current.Count < Math.Min(equipments.Count, slotCount))
                {
                    return true;
                }

                // When any other equipments are stronger than current one.
                foreach (var equipment in equipments)
                {
                    if (equipment.equipped)
                    {
                        continue;
                    }

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
