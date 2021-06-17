using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class ShardedShopStateV2 : State
    {
        public static Address DeriveAddress(ItemSubType itemSubType, Guid orderId)
        {
            string nonce = orderId.ToString().Substring(0, 1);
            return DeriveAddress(itemSubType, nonce);
        }

        public static Address DeriveAddress(ItemSubType itemSubType, string nonce)
        {
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                case ItemSubType.Food:
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return Addresses.Shop.Derive($"order-{itemSubType}-{nonce}");
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    return Addresses.Shop.Derive($"order-{itemSubType}");
                default:
                    throw new InvalidItemTypeException($"Unsupported ItemType: {itemSubType}");
            }
        }

        public IReadOnlyList<OrderDigest> OrderDigestList => _orderDigestList;
        private List<OrderDigest> _orderDigestList = new List<OrderDigest>();

        public ShardedShopStateV2(Address address) : base(address)
        {
        }

        public ShardedShopStateV2(Dictionary serialized) : base(serialized)
        {
            _orderDigestList = serialized[OrderDigestListKey]
                .ToList(s => new OrderDigest((Dictionary) s));
        }

        public void Add(OrderDigest orderDigest, long blockIndex)
        {
            if (_orderDigestList.Exists(o => o.OrderId.Equals(orderDigest.OrderId)))
            {
                throw new DuplicateOrderIdException($"{orderDigest.OrderId} Already Exist.");
            }
            _orderDigestList.Add(orderDigest);
            CleanUp(blockIndex);
        }

        public void Remove(Order order, long blockIndex)
        {
            OrderDigest orderDigest = _orderDigestList
                .FirstOrDefault(o =>
                    o.OrderId.Equals(order.OrderId) &&
                    o.SellerAgentAddress.Equals(order.SellerAgentAddress) &&
                    o.TradableId.Equals(order.TradableId)
                );
            if (orderDigest is null)
            {
                throw new OrderIdDoesNotExistException($"Can't find {nameof(OrderDigest)}: {order.OrderId}");
            }
            _orderDigestList.Remove(orderDigest);
            CleanUp(blockIndex);
        }

        private void CleanUp(long blockIndex)
        {
            _orderDigestList = OrderDigestList
                .Where(o => o.ExpiredBlockIndex >= blockIndex)
                .OrderBy(o => o.StartedBlockIndex).ToList();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) OrderDigestListKey] = new List(OrderDigestList.Select(o => o.Serialize()))
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
