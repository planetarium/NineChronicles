using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    public static class OrderFactory
    {
        public static Order Create(Address agentAddress, Address avatarAddress, Guid orderId,
            FungibleAssetValue price, Guid tradableId, long startedIndex, ItemSubType itemSubType, int count)
        {
            switch (itemSubType)
            {
                case ItemSubType.Food:
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                case ItemSubType.Title:
                    return CreateNonFungibleOrder(agentAddress, avatarAddress, orderId, price, tradableId,
                        startedIndex, itemSubType);
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return CreateFungibleOrder(agentAddress, avatarAddress, orderId, price, tradableId,
                        startedIndex, count, itemSubType);
                default:
                    throw new InvalidItemTypeException($"{nameof(itemSubType)}({itemSubType}) does not support.");
            }
        }

        public static NonFungibleOrder CreateNonFungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            ItemSubType itemSubType
        )
        {
            return new NonFungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, startedBlockIndex, itemSubType);
        }

        public static FungibleOrder CreateFungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            int count, ItemSubType itemSubType)
        {
            return new FungibleOrder(sellerAgentAddress, sellerAvatarAddress, orderId, price, itemId, startedBlockIndex, itemSubType, count);
        }

        public static Order Deserialize(Dictionary dictionary)
        {
            return dictionary[OrderTypeKey].ToEnum<Order.OrderType>().Equals(Order.OrderType.Fungible)
                ? (Order) new FungibleOrder(dictionary)
                : new NonFungibleOrder(dictionary);
        }

    }
}
