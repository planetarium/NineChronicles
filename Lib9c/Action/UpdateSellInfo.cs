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

        public UpdateSellInfo(Dictionary serialized)
        {
            orderId = serialized[OrderIdKey].ToGuid();
            updateSellOrderId = serialized[updateSellOrderIdKey].ToGuid();
            tradableId = serialized[ItemIdKey].ToGuid();
            itemSubType = (ItemSubType)serialized[ItemSubTypeKey].ToInteger();
            price = serialized[PriceKey].ToFungibleAssetValue();
            count = serialized[ItemCountKey].ToInteger();
        }
        
        public IValue Serialize()
        {
            var dictionary = new Dictionary<IKey, IValue>
            {
                [(Text) OrderIdKey] = orderId.Serialize(),
                [(Text) updateSellOrderIdKey] = updateSellOrderId.Serialize(),
                [(Text) ItemIdKey] = tradableId.Serialize(),
                [(Text) ItemSubTypeKey] = itemSubType.Serialize(),
                [(Text) PriceKey] = price.Serialize(),
                [(Text) ItemCountKey] = count.Serialize()
            };
            return new Dictionary(dictionary);
        }
    }
}