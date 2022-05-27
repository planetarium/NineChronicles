using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    public class UpdateSellInfo
    {
        public Guid orderId;
        public Guid updateSellOrderId;
        public Guid tradableId;
        public ItemSubType itemSubType;
        public FungibleAssetValue price;
        public int count;

        public UpdateSellInfo(
            Guid orderId,
            Guid updateSellOrderId,
            Guid tradableId,
            ItemSubType itemSubType,
            FungibleAssetValue price,
            int count)
        {
            this.orderId = orderId;
            this.updateSellOrderId = updateSellOrderId;
            this.tradableId = tradableId;
            this.itemSubType = itemSubType;
            this.price = price;
            this.count = count;
        }

        public UpdateSellInfo(List serialized)
        {
            orderId = serialized[0].ToGuid();
            updateSellOrderId = serialized[1].ToGuid();
            tradableId = serialized[2].ToGuid();
            itemSubType = serialized[3].ToEnum<ItemSubType>();
            price = serialized[4].ToFungibleAssetValue();
            count = serialized[5].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(orderId.Serialize())
                .Add(updateSellOrderId.Serialize())
                .Add(tradableId.Serialize())
                .Add(itemSubType.Serialize())
                .Add(price.Serialize())
                .Add(count.Serialize());
        }
    }
}
