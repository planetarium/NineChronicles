using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Model.Order;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class ShopEquipments
    {
        public List<ShopEquipment> shopEquipments;
    }

    [Serializable]
    public class ShopEquipment
    {
        public string orderId;
        public string tradableId;
        public long sellStartedBlockIndex;
        public long sellExpiredBlockIndex;
        public string sellerAgentAddress;
        public string sellerAvatarAddress;
        public decimal price;
        public long combatPoint;
        public int level;
        public int id;
        public int itemCount;
        public string itemSubType;

        public OrderDigest ToOrderDigest(Currency currency)
        {
            return new OrderDigest(new Address(sellerAgentAddress), sellStartedBlockIndex,
                sellExpiredBlockIndex, Guid.Parse(orderId), Guid.Parse(tradableId),
                (BigInteger)price * currency, combatPoint, level, id, itemCount);
        }
    }

    [Serializable]
    public class ShopResponse
    {
        public ShopEquipments shopQuery;
    }
}
