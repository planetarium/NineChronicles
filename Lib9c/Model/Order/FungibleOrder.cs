using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class FungibleOrder : Order
    {
        public readonly int ItemCount;

        public FungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid tradableId,
            long startedBlockIndex,
            ItemSubType itemSubType,
            int itemCount
        ) : base(sellerAgentAddress,
            sellerAvatarAddress,
            orderId,
            price,
            tradableId,
            startedBlockIndex,
            itemSubType
        )
        {
            ItemCount = itemCount;
        }

        public FungibleOrder(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized[ItemCountKey].ToInteger();
        }

        public override OrderType Type => OrderType.Fungible;

        public override IValue Serialize() => ((Dictionary) base.Serialize())
            .SetItem(ItemCountKey, ItemCount.Serialize());

        public override void Validate(AvatarState avatarState, int count)
        {
            base.Validate(avatarState, count);

            if (ItemCount != count)
            {
                throw new InvalidItemCountException(
                    $"Aborted because {nameof(count)}({count}) should be 1 because {nameof(TradableId)}({TradableId}) is non-fungible item.");
            }

            if (!avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, count, out List<Inventory.Item> inventoryItems))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            IEnumerable<ITradableItem> tradableItems = inventoryItems.Select(i => (ITradableItem)i.item).ToList();

            foreach (var tradableItem in tradableItems)
            {
                if (!tradableItem.ItemSubType.Equals(ItemSubType))
                {
                    throw new InvalidItemTypeException(
                        $"Expected ItemSubType: {tradableItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
                }
            }
        }

        public override ITradableItem Sell(AvatarState avatarState)
        {
            if (!avatarState.inventory.TryGetTradableItems(
                    TradableId,
                    StartedBlockIndex,
                    ItemCount,
                    out List<Inventory.Item> items))
            {
                throw new ItemDoesNotExistException(
                    $"Can't find available item in seller inventory. TradableId: {TradableId}. RequiredBlockIndex: {StartedBlockIndex}, Count: {ItemCount}");
            }

            var totalCount = ItemCount;
            // Copy ITradableFungible item for separate inventory slots.
            var copy = (ITradableFungibleItem) ((ITradableFungibleItem) items.First().item).Clone();
            foreach (var item in items)
            {
                var removeCount = Math.Min(totalCount, item.count);
                var tradableFungibleItem = (ITradableFungibleItem) item.item;
                if (!avatarState.inventory.RemoveTradableItem(
                        TradableId,
                        tradableFungibleItem.RequiredBlockIndex,
                        removeCount))
                {
                    throw new Exception("Aborted because failed to remove item from inventory.");
                }

                totalCount -= removeCount;
                if (totalCount < 1)
                {
                    break;
                }
            }

            if (totalCount != 0)
            {
                throw new Exception("Aborted because failed to remove item from inventory.");
            }

            // Lock item.
            copy.RequiredBlockIndex = ExpiredBlockIndex;
            avatarState.inventory.AddItem((ItemBase) copy, ItemCount, new OrderLock(OrderId));
            return copy;
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell2(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, ItemCount, out List<Inventory.Item> items))
            {
                int totalCount = ItemCount;
                // Copy ITradableFungible item for separate inventory slots.
                ITradableFungibleItem copy = (ITradableFungibleItem) ((ITradableFungibleItem) items.First().item).Clone();
                foreach (var item in items)
                {
                    int removeCount = Math.Min(totalCount, item.count);
                    ITradableFungibleItem tradableFungibleItem = (ITradableFungibleItem) item.item;
                    avatarState.inventory.RemoveTradableItemV1(TradableId, tradableFungibleItem.RequiredBlockIndex, removeCount);
                    totalCount -= removeCount;
                    if (totalCount < 1)
                    {
                        break;
                    }
                }
                // Lock item.
                copy.RequiredBlockIndex = ExpiredBlockIndex;
                avatarState.inventory.AddItem2((ItemBase) copy, ItemCount);
                return copy;
            }

            throw new ItemDoesNotExistException(
                $"Can't find available item in seller inventory. TradableId: {TradableId}. RequiredBlockIndex: {StartedBlockIndex}, Count: {ItemCount}");
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell3(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, ItemCount, out List<Inventory.Item> items))
            {
                int totalCount = ItemCount;
                // Copy ITradableFungible item for separate inventory slots.
                ITradableFungibleItem copy = (ITradableFungibleItem) ((ITradableFungibleItem) items.First().item).Clone();
                foreach (var item in items)
                {
                    int removeCount = Math.Min(totalCount, item.count);
                    ITradableFungibleItem tradableFungibleItem = (ITradableFungibleItem) item.item;
                    avatarState.inventory.RemoveTradableItemV1(TradableId, tradableFungibleItem.RequiredBlockIndex, removeCount);
                    totalCount -= removeCount;
                    if (totalCount < 1)
                    {
                        break;
                    }
                }
                // Lock item.
                copy.RequiredBlockIndex = ExpiredBlockIndex;
                avatarState.inventory.AddItem2((ItemBase) copy, ItemCount, new OrderLock(OrderId));
                return copy;
            }

            throw new ItemDoesNotExistException(
                $"Can't find available item in seller inventory. TradableId: {TradableId}. RequiredBlockIndex: {StartedBlockIndex}, Count: {ItemCount}");
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell4(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, ItemCount, out List<Inventory.Item> items))
            {
                int totalCount = ItemCount;
                // Copy ITradableFungible item for separate inventory slots.
                ITradableFungibleItem copy = (ITradableFungibleItem) ((ITradableFungibleItem) items.First().item).Clone();
                foreach (var item in items)
                {
                    int removeCount = Math.Min(totalCount, item.count);
                    ITradableFungibleItem tradableFungibleItem = (ITradableFungibleItem) item.item;
                    avatarState.inventory.RemoveTradableItemV1(TradableId, tradableFungibleItem.RequiredBlockIndex, removeCount);
                    totalCount -= removeCount;
                    if (totalCount < 1)
                    {
                        break;
                    }
                }
                // Lock item.
                copy.RequiredBlockIndex = ExpiredBlockIndex;
                avatarState.inventory.AddItem((ItemBase) copy, ItemCount, new OrderLock(OrderId));
                return copy;
            }

            throw new ItemDoesNotExistException(
                $"Can't find available item in seller inventory. TradableId: {TradableId}. RequiredBlockIndex: {StartedBlockIndex}, Count: {ItemCount}");
        }

        public override OrderDigest Digest(AvatarState avatarState, CostumeStatSheet costumeStatSheet)
        {
            if (avatarState.inventory.TryGetLockedItem(new OrderLock(OrderId), out Inventory.Item inventoryItem))
            {
                ItemBase item = inventoryItem.item;
                int cp = CPHelper.GetCP((ITradableItem) item, costumeStatSheet);
                int level = item is Equipment equipment ? equipment.level : 0;
                return new OrderDigest(
                    SellerAgentAddress,
                    StartedBlockIndex,
                    ExpiredBlockIndex,
                    OrderId,
                    TradableId,
                    Price,
                    cp,
                    level,
                    item.Id,
                    ItemCount
                );
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        public override OrderReceipt Transfer(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                var tradableItem = (TradableMaterial) inventoryItem.item;
                seller.inventory.RemoveItem(inventoryItem);

                var copy = (TradableMaterial) tradableItem.Clone();
                copy.RequiredBlockIndex = blockIndex;
                buyer.UpdateFromAddItem(copy, ItemCount, false);
                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from seller's inventory.");
        }

        [Obsolete("Use Transfer")]
        public override OrderReceipt Transfer2(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetTradableItem(TradableId, ExpiredBlockIndex, ItemCount,
                out Inventory.Item inventoryItem))
            {
                TradableMaterial tradableItem = (TradableMaterial) inventoryItem.item;
                seller.inventory.RemoveTradableItemV1(tradableItem, ItemCount);
                TradableMaterial copy = (TradableMaterial) tradableItem.Clone();
                copy.RequiredBlockIndex = blockIndex;
                buyer.UpdateFromAddItem2(copy, ItemCount, false);
                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }
            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from seller's inventory.");
        }

        [Obsolete("Use Transfer")]
        public override OrderReceipt Transfer3(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                var tradableItem = (TradableMaterial) inventoryItem.item;
                seller.inventory.RemoveItem(inventoryItem);

                var copy = (TradableMaterial) tradableItem.Clone();
                copy.RequiredBlockIndex = blockIndex;
                buyer.UpdateFromAddItem2(copy, ItemCount, false);
                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from seller's inventory.");
        }

        public override int ValidateTransfer(AvatarState avatarState, Guid tradableId,
            FungibleAssetValue price, long blockIndex)
        {
            var errorCode =  base.ValidateTransfer(avatarState, tradableId, price, blockIndex);
            if (errorCode != 0)
            {
                return errorCode;
            }

            if (!avatarState.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                return Buy.ErrorCodeItemDoesNotExist;
            }

            if (!inventoryItem.count.Equals(ItemCount))
            {
                return Buy.ErrorCodeItemDoesNotExist;
            }

            if (inventoryItem.item is ITradableItem tradableItem)
            {
                return tradableItem.ItemSubType.Equals(ItemSubType) ? errorCode : Buy.ErrorCodeInvalidItemType;
            }

            return Buy.ErrorCodeItemDoesNotExist;
        }

        public override void ValidateCancelOrder(AvatarState avatarState, Guid tradableId)
        {
            base.ValidateCancelOrder(avatarState, tradableId);

            if (!avatarState.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            if (inventoryItem.count != ItemCount)
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            var tradableItem = (ITradableItem)inventoryItem.item;
            if (!tradableItem.ItemSubType.Equals(ItemSubType))
            {
                throw new InvalidItemTypeException(
                    $"Expected ItemSubType: {tradableItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
            }
        }

        [Obsolete("Use ValidateCancelOrder")]
        public override void ValidateCancelOrder2(AvatarState avatarState, Guid tradableId)
        {
            base.ValidateCancelOrder2(avatarState, tradableId);

            if (!avatarState.inventory.TryGetTradableItems(TradableId, ExpiredBlockIndex, ItemCount, out List<Inventory.Item> inventoryItems))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            IEnumerable<ITradableItem> tradableItems = inventoryItems.Select(i => (ITradableItem)i.item).ToList();

            foreach (var tradableItem in tradableItems)
            {
                if (!tradableItem.ItemSubType.Equals(ItemSubType))
                {
                    throw new InvalidItemTypeException(
                        $"Expected ItemSubType: {tradableItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
                }
            }
        }



        [Obsolete("Use Digest")]
        public override OrderDigest Digest2(AvatarState avatarState, CostumeStatSheet costumeStatSheet)
        {
            if (avatarState.inventory.TryGetTradableItem(TradableId, ExpiredBlockIndex, ItemCount,
                out Inventory.Item inventoryItem))
            {
                ItemBase item = inventoryItem.item;
                int cp = CPHelper.GetCP((ITradableItem) item, costumeStatSheet);
                int level = item is Equipment equipment ? equipment.level : 0;
                return new OrderDigest(
                    SellerAgentAddress,
                    StartedBlockIndex,
                    ExpiredBlockIndex,
                    OrderId,
                    TradableId,
                    Price,
                    cp,
                    level,
                    item.Id,
                    ItemCount
                );
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use ValidateTransfer")]
        public override int ValidateTransfer2(AvatarState avatarState, Guid tradableId, FungibleAssetValue price, long blockIndex)
        {
            var errorCode =  base.ValidateTransfer2(avatarState, tradableId, price, blockIndex);
            if (errorCode != 0)
            {
                return errorCode;
            }

            if (!avatarState.inventory.TryGetTradableItems(TradableId, ExpiredBlockIndex, ItemCount, out List<Inventory.Item> inventoryItems))
            {
                return Buy.ErrorCodeItemDoesNotExist;
            }

            IEnumerable<ITradableItem> tradableItems = inventoryItems.Select(i => (ITradableItem)i.item).ToList();

            return tradableItems.Any(tradableItem => !tradableItem.ItemSubType.Equals(ItemSubType))
                ? Buy.ErrorCodeInvalidItemType
                : errorCode;
        }

        [Obsolete("Use Cancel")]
        public override ITradableItem Cancel2(AvatarState avatarState, long blockIndex)
        {
            if (avatarState.inventory.TryGetTradableItem(TradableId, ExpiredBlockIndex, ItemCount,
                out Inventory.Item inventoryItem))
            {
                ITradableFungibleItem copy = (ITradableFungibleItem) ((ITradableFungibleItem) inventoryItem.item).Clone();
                avatarState.inventory.RemoveTradableItemV1(TradableId, ExpiredBlockIndex, ItemCount);
                copy.RequiredBlockIndex = blockIndex;
                avatarState.inventory.AddItem2((ItemBase) copy, ItemCount);
                return copy;
            }
            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        protected bool Equals(FungibleOrder other)
        {
            return base.Equals(other) && ItemCount == other.ItemCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FungibleOrder) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemCount;
            }
        }
    }
}
