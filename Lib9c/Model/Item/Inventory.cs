using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
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
            if (!(itemBase is IFungibleItem fungibleItem))
            {
                throw new ArgumentException(
                    $"Aborted because {nameof(itemBase)} cannot cast to {nameof(IFungibleItem)}");
            }

            var item = _items.FirstOrDefault(e => e.item.Equals(fungibleItem));
            if (item is null)
            {
                item = new Item(itemBase, count);
                _items.Add(item);
            }
            else
            {
                item.count += count;
            }

            return item;
        }

        public Item AddNonFungibleItem(ItemBase itemBase)
        {
            var nonFungibleItem = new Item(itemBase);
            _items.Add(nonFungibleItem);
            return nonFungibleItem;
        }

        #endregion

        #region Remove

        public bool RemoveFungibleItem(
            IFungibleItem fungibleItem,
            int count = 1,
            bool onlyTradableItem = default) =>
            RemoveFungibleItem(fungibleItem.FungibleId, count, onlyTradableItem);

        public bool RemoveFungibleItem(
            HashDigest<SHA256> fungibleId,
            int count = 1,
            bool onlyTradableItem = default)
        {
            var targetItems = (onlyTradableItem
                    ? _items
                        .Where(e =>
                            e.item is ITradableFungibleItem tradableFungibleItem &&
                            tradableFungibleItem.FungibleId.Equals(fungibleId))
                    : _items
                        .Where(e =>
                            e.item is IFungibleItem ownedFungibleItem &&
                            ownedFungibleItem.FungibleId.Equals(fungibleId))
                        .OrderBy(e => e.item is ITradableItem))
                .ToArray();
            if (targetItems.Length == 0)
            {
                return false;
            }

            var totalCount = targetItems.Sum(e => e.count);
            if (totalCount < count)
            {
                return false;
            }

            for (var i = 0; i < targetItems.Length; i++)
            {
                var item = targetItems[i];
                if (item.count > count)
                {
                    item.count -= count;
                    break;
                }

                count -= item.count;
                item.count = 0;
                _items.Remove(item);
            }

            return true;
        }

        public bool RemoveNonFungibleItem(INonFungibleItem nonFungibleItem)
            => RemoveNonFungibleItem(nonFungibleItem.NonFungibleId);

        public bool RemoveNonFungibleItem(Guid nonFungibleId)
            => TryGetNonFungibleItem(nonFungibleId, out var item) && _items.Remove(item);

        public bool RemoveTradableItem(ITradableItem tradableItem, int count = 1)
        {
            switch (tradableItem)
            {
                case IFungibleItem fungibleItem:
                    return RemoveFungibleItem(fungibleItem, count, true);
                case INonFungibleItem nonFungibleItem:
                    return RemoveNonFungibleItem(nonFungibleItem);
                default:
                    return false;
            }
        }

        public bool RemoveTradableFungibleItem(HashDigest<SHA256> fungibleId, int count = 1) =>
            RemoveFungibleItem(fungibleId, count, true);

        [Obsolete("Use RemoveNonFungibleItem(INonFungibleItem nonFungibleItem)")]
        public bool LegacyRemoveNonFungibleItem(Costume costume)
            => LegacyRemoveNonFungibleItem(costume.ItemId);

        [Obsolete("Use RemoveNonFungibleItem(Guid itemId)")]
        public bool LegacyRemoveNonFungibleItem(Guid nonFungibleId)
        {
            var isRemoved = TryGetNonFungibleItem(nonFungibleId, out Item item);
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

        public bool TryGetNonFungibleItem(Guid nonFungibleId, out Item outItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is INonFungibleItem nonFungibleItem) ||
                    !nonFungibleItem.NonFungibleId.Equals(nonFungibleId))
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

        public bool TryGetTradableItem(Guid tradeId, out Item outItem)
        {
            foreach (var item in _items)
            {
                if (!(item.item is ITradableItem tradableItem) ||
                    !tradableItem.TradableId.Equals(tradeId))
                {
                    continue;
                }

                outItem = item;
                return true;
            }

            outItem = null;
            return false;
        }

        // public bool TryGetTradableItemWithoutNonTradableFungibleItem(
        //     Guid tradeId,
        //     out Item outItem)
        // {
        //     foreach (var item in _items)
        //     {
        //         if (!(item.item is ITradableItem tradableItem) ||
        //             !tradableItem.TradableId.Equals(tradeId))
        //         {
        //             continue;
        //         }
        //
        //         if (tradableItem is IFungibleItem fungibleItem &&
        //             !(fungibleItem is ITradableFungibleItem))
        //         {
        //             continue;
        //         }
        //
        //         outItem = item;
        //         return true;
        //     }
        //
        //     outItem = null;
        //     return false;
        // }

        // public bool TryGetNonTradableFungibleItem(
        //     HashDigest<SHA256> fungibleId,
        //     out Item outItem)
        // {
        //     foreach (var item in _items)
        //     {
        //         if (!(item.item is IFungibleItem fungibleItem) ||
        //             fungibleItem is ITradableFungibleItem ||
        //             !fungibleItem.FungibleId.Equals(fungibleId))
        //         {
        //             continue;
        //         }
        //
        //         outItem = item;
        //         return true;
        //     }
        //
        //     outItem = null;
        //     return false;
        // }

        #endregion

        #region Has

        public bool HasItem(int rowId, int count = 1) => _items
            .Exists(item =>
                item.item.Id == rowId &&
                item.count >= count);

        public bool HasFungibleItem(HashDigest<SHA256> fungibleId, int count = 1) => _items
            .Exists(item =>
                item.item is IFungibleItem fungibleItem &&
                fungibleItem.FungibleId.Equals(fungibleId) &&
                item.count >= count);

        public bool HasNonFungibleItem(Guid nonFungibleId) => _items
            .Select(i => i.item)
            .OfType<INonFungibleItem>()
            .Any(i => i.NonFungibleId.Equals(nonFungibleId));

        public bool HasTradableItem(Guid tradableId) => _items
            .Select(i => i.item)
            .OfType<ITradableItem>()
            .Any(i => i.TradableId.Equals(tradableId));

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
