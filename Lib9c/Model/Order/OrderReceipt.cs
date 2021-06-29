using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderReceipt
    {
        public static Address DeriveAddress(Guid orderId)
        {
            return Order.DeriveAddress(orderId).Derive(nameof(OrderReceipt));
        }

        public readonly Guid OrderId;
        public readonly Address BuyerAgentAddress;
        public readonly Address BuyerAvatarAddress;
        public readonly long TransferredBlockIndex;

        public OrderReceipt(Guid orderId, Address buyerAgentAddress, Address buyerAvatarAddress, long transferredBlockIndex)
        {
            OrderId = orderId;
            BuyerAgentAddress = buyerAgentAddress;
            BuyerAvatarAddress = buyerAvatarAddress;
            TransferredBlockIndex = transferredBlockIndex;
        }

        public OrderReceipt(Dictionary serialized)
        {
            OrderId = serialized[OrderIdKey].ToGuid();
            BuyerAgentAddress = serialized[BuyerAgentAddressKey].ToAddress();
            BuyerAvatarAddress = serialized[BuyerAvatarAddressKey].ToAddress();
            TransferredBlockIndex = serialized[BlockIndexKey].ToLong();
        }

        public IValue Serialize()
        {
            return Dictionary.Empty
                .Add(OrderIdKey, OrderId.Serialize())
                .Add(BuyerAgentAddressKey, BuyerAgentAddress.Serialize())
                .Add(BuyerAvatarAddressKey, BuyerAvatarAddress.Serialize())
                .Add(BlockIndexKey, TransferredBlockIndex.Serialize());
        }
    }
}
