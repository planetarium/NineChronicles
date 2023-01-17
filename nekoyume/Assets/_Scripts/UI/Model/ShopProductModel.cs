using System;
using System.Collections.Generic;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Newtonsoft.Json;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class ShopProductModel
    {
        public int Id { get; set; }
        public Address SellerAgentAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public int CombatPoint { get; set; }
        public int Level { get; set; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public Guid OrderId { get; set; }
        public Guid TradableId { get; set; }
        public long StartedBlockIndex { get; set; }
        public long ExpiredBlockIndex { get; set; }
        public ItemSubType ItemSubType { get; set; }

        public OrderDigest ToOrderDigest()
        {
            return new OrderDigest(
                SellerAgentAddress,
                StartedBlockIndex,
                ExpiredBlockIndex,
                OrderId,
                TradableId,
                Price,
                CombatPoint,
                Level,
                ItemId,
                ItemCount
            );
        }
    }

    [Serializable]
    public class ProductResponse
    {
        public int Limit { get; set; }
        public int Offset { get; set; }
        public IEnumerable<ShopProductModel> Products { get; set; }
    }
}
