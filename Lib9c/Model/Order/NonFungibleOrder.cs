using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class NonFungibleOrder : Order
    {
        public NonFungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid tradableId,
            long startedBlockIndex,
            ItemSubType itemSubType
        ) : base(
            sellerAgentAddress,
            sellerAvatarAddress,
            orderId,
            price,
            tradableId,
            startedBlockIndex,
            itemSubType
        )
        {
        }

        public NonFungibleOrder(Dictionary serialized) : base(serialized)
        {
        }

        public override OrderType Type => OrderType.NonFungible;

        public override void Validate(AvatarState avatarState, int count)
        {
            base.Validate(avatarState, count);

            if (count != 1)
            {
                throw new InvalidItemCountException(
                    $"Aborted because {nameof(count)}({count}) should be 1 because {nameof(TradableId)}({TradableId}) is non-fungible item.");
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            if (!nonFungibleItem.ItemSubType.Equals(ItemSubType))
            {
                throw new InvalidItemTypeException(
                    $"Expected ItemSubType: {nonFungibleItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
            }

            if (nonFungibleItem.RequiredBlockIndex > StartedBlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"Aborted as the itemUsable to sell ({TradableId}) is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}.");
            }
        }

        public override ITradableItem Sell(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out Inventory.Item inventoryItem))
            {
                inventoryItem.LockUp(new OrderLock(OrderId));
                INonFungibleItem nonFungibleItem = (INonFungibleItem)inventoryItem.item;
                nonFungibleItem.RequiredBlockIndex = ExpiredBlockIndex;
                if (nonFungibleItem is IEquippableItem equippableItem)
                {
                    equippableItem.Unequip();
                }

                return nonFungibleItem;
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell2(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                nonFungibleItem.RequiredBlockIndex = ExpiredBlockIndex;
                if (nonFungibleItem is IEquippableItem equippableItem)
                {
                    equippableItem.Unequip();
                }

                return nonFungibleItem;
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell3(AvatarState avatarState)
        {
            return Sell(avatarState);
        }

        [Obsolete("Use Sell")]
        public override ITradableItem Sell4(AvatarState avatarState)
        {
            return Sell(avatarState);
        }

        public override OrderDigest Digest(AvatarState avatarState, CostumeStatSheet costumeStatSheet)
        {
            if (avatarState.inventory.TryGetLockedItem(new OrderLock(OrderId), out Inventory.Item inventoryItem))
            {
                ItemBase item = inventoryItem.item;
                int cp = CPHelper.GetCP((INonFungibleItem)item, costumeStatSheet);
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
                    1
                );
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        public override OrderReceipt Transfer(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                if (inventoryItem.item is INonFungibleItem nonFungibleItem)
                {
                    nonFungibleItem.RequiredBlockIndex = blockIndex;
                    seller.inventory.RemoveItem(inventoryItem);

                    if (nonFungibleItem is Costume costume)
                    {
                        buyer.UpdateFromAddCostume(costume, false);
                    }
                    else
                    {
                        buyer.UpdateFromAddItem((ItemUsable)nonFungibleItem, false);
                    }
                }

                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use Transfer")]
        public override OrderReceipt Transfer2(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                seller.inventory.RemoveNonFungibleItem(TradableId);
                nonFungibleItem.RequiredBlockIndex = blockIndex;
                if (nonFungibleItem is Costume costume)
                {
                    buyer.UpdateFromAddCostume(costume, false);
                }
                else
                {
                    buyer.UpdateFromAddItem2((ItemUsable) nonFungibleItem, false);
                }

                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use Transfer")]
        public override OrderReceipt Transfer3(AvatarState seller, AvatarState buyer, long blockIndex)
        {
            if (seller.inventory.TryGetLockedItem(new OrderLock(OrderId), out var inventoryItem))
            {
                if (inventoryItem.item is INonFungibleItem nonFungibleItem)
                {
                    nonFungibleItem.RequiredBlockIndex = blockIndex;
                    seller.inventory.RemoveItem(inventoryItem);

                    if (nonFungibleItem is Costume costume)
                    {
                        buyer.UpdateFromAddCostume(costume, false);
                    }
                    else
                    {
                        buyer.UpdateFromAddItem2((ItemUsable)nonFungibleItem, false);
                    }
                }

                return new OrderReceipt(OrderId, buyer.agentAddress, buyer.address, blockIndex);
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        public override int ValidateTransfer(AvatarState avatarState, Guid tradableId, FungibleAssetValue price, long blockIndex)
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

            if (inventoryItem.item is INonFungibleItem nonFungibleItem)
            {
                return nonFungibleItem.ItemSubType.Equals(ItemSubType) ? errorCode : Buy.ErrorCodeInvalidItemType;
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

            if (inventoryItem.count != 1)
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



        [Obsolete("Use Digest")]
        public override OrderDigest Digest2(AvatarState avatarState, CostumeStatSheet costumeStatSheet)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                ItemBase item = (ItemBase) nonFungibleItem;
                int cp = CPHelper.GetCP(nonFungibleItem, costumeStatSheet);
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
                    1
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

            if (!avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                return Buy.ErrorCodeItemDoesNotExist;
            }

            return !nonFungibleItem.ItemSubType.Equals(ItemSubType) ? Buy.ErrorCodeInvalidItemType : errorCode;
        }

        [Obsolete("Use Cancel")]
        public override ITradableItem Cancel2(AvatarState avatarState, long blockIndex)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                nonFungibleItem.RequiredBlockIndex = blockIndex;
                return nonFungibleItem;
            }
            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        [Obsolete("Use ValidateCancelOrder")]
        public override void ValidateCancelOrder2(AvatarState avatarState, Guid tradableId)
        {
            base.ValidateCancelOrder2(avatarState, tradableId);

            if (!avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            if (!nonFungibleItem.ItemSubType.Equals(ItemSubType))
            {
                throw new InvalidItemTypeException(
                    $"Expected ItemSubType: {nonFungibleItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
            }
        }
    }
}
