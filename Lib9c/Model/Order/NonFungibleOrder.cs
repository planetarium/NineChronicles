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
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                nonFungibleItem.RequiredBlockIndex = ExpiredBlockIndex;
                if (nonFungibleItem is Equipment equipment)
                {
                    equipment.Unequip();
                }
                return nonFungibleItem;
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        public override OrderDigest Digest(AvatarState avatarState, CostumeStatSheet costumeStatSheet)
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

        public override void ValidateCancelOrder(AvatarState avatarState, Guid tradableId)
        {
            base.ValidateCancelOrder(avatarState, tradableId);

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

        public override ITradableItem Cancel(AvatarState avatarState, long blockIndex)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                nonFungibleItem.RequiredBlockIndex = blockIndex;
                return nonFungibleItem;
            }
            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }

        public override int ValidateTransfer(AvatarState avatarState, Guid tradableId, FungibleAssetValue price, long blockIndex)
        {
            int errorCode =  base.ValidateTransfer(avatarState, tradableId, price, blockIndex);
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
    }
}
