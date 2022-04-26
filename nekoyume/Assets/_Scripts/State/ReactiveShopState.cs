using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.State
{
    /// <summary>
    /// Changes in the values included in ShopState are notified to the outside through each ReactiveProperty<T> field.
    /// </summary>
    public static class ReactiveShopState
    {
        private static readonly List<ItemSubType> ItemSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.FullCostume,
            ItemSubType.HairCostume,
            ItemSubType.EarCostume,
            ItemSubType.EyeCostume,
            ItemSubType.TailCostume,
            ItemSubType.Title,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        private static readonly List<ItemSubType> ShardedSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        public static ReactiveProperty<List<OrderDigest>> BuyDigest { get; } =
            new ReactiveProperty<List<OrderDigest>>();

        public static ReactiveProperty<List<OrderDigest>> SellDigest { get; } =
            new ReactiveProperty<List<OrderDigest>>();

        // key: orderId
        private static ConcurrentDictionary<Guid, ItemBase> CachedShopItems { get; } =
            new ConcurrentDictionary<Guid, ItemBase>();


        private static readonly Dictionary<ItemSubType, List<OrderDigest>> _buyDigest =
            new Dictionary<ItemSubType, List<OrderDigest>>();
        private static List<Guid> _removedOrderIds { get; } = new List<Guid>();

        public static OrderDigest GetSellDigest(Guid tradableId,
            long requiredBlockIndex,
            FungibleAssetValue price,
            int count)
        {
            return SellDigest.Value.FirstOrDefault(x =>
                x.TradableId.Equals(tradableId) &&
                x.ExpiredBlockIndex.Equals(requiredBlockIndex) &&
                x.Price.Equals(price) &&
                x.ItemCount.Equals(count));
        }

        public static bool TryGetShopItem(OrderDigest orderDigest, out ItemBase itemBase)
        {
            if (!CachedShopItems.ContainsKey(orderDigest.OrderId))
            {
                Debug.LogWarning(
                    $"[{nameof(TryGetShopItem)}] Not found address: {orderDigest.OrderId}");
                itemBase = null;
                return false;
            }

            itemBase = CachedShopItems[orderDigest.OrderId];
            return true;
        }

        public static async Task SetBuyDigests(List<ItemSubType> list)
        {
            await UniTask.Run(async () =>
            {
                _removedOrderIds.Clear();

                foreach (var itemSubType in list)
                {
                    var digests = await GetBuyOrderDigests(itemSubType);
                    var result = await UpdateCachedShopItems(digests);
                    if (result)
                    {
                        AddBuyDigest(digests, itemSubType);
                    }
                }
                return true;
            });
        }

        private static void AddBuyDigest(IEnumerable<OrderDigest> digests, ItemSubType itemSubType)
        {
            var agentAddress = States.Instance.AgentState.address;
            var d = digests
                .Where(digest => !digest.SellerAgentAddress.Equals(agentAddress)).ToList();
            if (!_buyDigest.ContainsKey(itemSubType))
            {
                _buyDigest.Add(itemSubType, new List<OrderDigest>());
            }

            _buyDigest[itemSubType] = d;

            var buyDigests = new List<OrderDigest>();
            foreach (var pair in _buyDigest)
            {
                buyDigests.AddRange(pair.Value);
            }

            var removeList = buyDigests.Where(digest => _removedOrderIds.Contains(digest.OrderId)).ToList();
            foreach (var orderDigest in removeList)
            {
                buyDigests.Remove(orderDigest);
            }

            BuyDigest.Value = buyDigests;
        }

        public static async Task UpdateSellDigests()
        {
            var digests = await GetSellOrderDigests();
            var result = await UpdateCachedShopItems(digests);
            if (result)
            {
                SellDigest.Value = digests;
            }
        }

        public static void RemoveBuyDigest(Guid orderId)
        {
            var item = BuyDigest.Value.FirstOrDefault(x => x.OrderId.Equals(orderId));
            if (item != null)
            {
                if (!_removedOrderIds.Contains(orderId))
                {
                    _removedOrderIds.Add(orderId);
                }

                BuyDigest.Value.Remove(item);
                BuyDigest.SetValueAndForceNotify(BuyDigest.Value);
            }
        }

        public static void RemoveSellDigest(Guid orderId)
        {
            var item = SellDigest.Value.FirstOrDefault(x => x.OrderId.Equals(orderId));
            if (item != null)
            {
                SellDigest.Value.Remove(item);
                SellDigest.SetValueAndForceNotify(SellDigest.Value);
            }
        }

        private static async Task<List<OrderDigest>> GetBuyOrderDigests(ItemSubType itemSubType)
        {
            var orderDigests = new Dictionary<Address, List<OrderDigest>>();
            var addressList = new List<Address>();

            if (ShardedSubTypes.Contains(itemSubType))
            {
                addressList.AddRange(ShardedShopState.AddressKeys.Select(addressKey =>
                    ShardedShopStateV2.DeriveAddress(itemSubType, addressKey)));
            }
            else
            {
                var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                addressList.Add(address);
            }

            var values = await Game.Game.instance.Agent.GetStateBulk(addressList);
            var shopStates = new List<ShardedShopStateV2>();
            foreach (var kv in values)
            {
                if (kv.Value is Dictionary shopDict)
                {
                    shopStates.Add(new ShardedShopStateV2(shopDict));
                }
            }

            AddOrderDigest(shopStates, orderDigests);

            var digests = new List<OrderDigest>();
            foreach (var items in orderDigests.Values)
            {
                digests.AddRange(items);
            }

            return digests;
        }

        private static async Task<List<OrderDigest>> GetBuyOrderDigests()
        {
            var orderDigests = new Dictionary<Address, List<OrderDigest>>();
            var addressList = new List<Address>();

            foreach (var itemSubType in ItemSubTypes)
            {
                if (ShardedSubTypes.Contains(itemSubType))
                {
                    addressList.AddRange(ShardedShopState.AddressKeys.Select(addressKey =>
                        ShardedShopStateV2.DeriveAddress(itemSubType, addressKey)));
                }
                else
                {
                    var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                    addressList.Add(address);
                }
            }

            var values = await Game.Game.instance.Agent.GetStateBulk(addressList);
            var shopStates = new List<ShardedShopStateV2>();
            foreach (var kv in values)
            {
                if (kv.Value is Dictionary shopDict)
                {
                    shopStates.Add(new ShardedShopStateV2(shopDict));
                }
            }

            AddOrderDigest(shopStates, orderDigests);

            var digests = new List<OrderDigest>();
            foreach (var items in orderDigests.Values)
            {
                digests.AddRange(items);
            }

            return digests;
        }

        private static void AddOrderDigest(List<ShardedShopStateV2> shopStates,
            IDictionary<Address, List<OrderDigest>> orderDigests)
        {
            foreach (var shopState in shopStates)
            {
                foreach (var orderDigest in shopState.OrderDigestList)
                {
                    if (orderDigest.ExpiredBlockIndex != 0 && orderDigest.ExpiredBlockIndex >
                        Game.Game.instance.Agent.BlockIndex)
                    {
                        var agentAddress = orderDigest.SellerAgentAddress;
                        if (!orderDigests.ContainsKey(agentAddress))
                        {
                            orderDigests[agentAddress] = new List<OrderDigest>();
                        }

                        orderDigests[agentAddress].Add(orderDigest);
                    }
                }
            }
        }

        private static async Task<List<OrderDigest>> GetSellOrderDigests()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var receiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            var receiptState = await Game.Game.instance.Agent.GetStateAsync(receiptAddress);
            var receipts = new List<OrderDigest>();
            if (receiptState is Dictionary dictionary)
            {
                var state = new OrderDigestListState(dictionary);

                var validOrderDigests = state.OrderDigestList.Where(x =>
                    x.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex);
                receipts.AddRange(validOrderDigests);

                var expiredOrderDigests = state.OrderDigestList.Where(x =>
                    x.ExpiredBlockIndex <= Game.Game.instance.Agent.BlockIndex);
                var inventory = States.Instance.CurrentAvatarState.inventory;
                var lockedDigests = expiredOrderDigests
                    .Where(x => inventory.TryGetLockedItem(new OrderLock(x.OrderId), out _))
                    .ToList();
                receipts.AddRange(lockedDigests);
            }

            return receipts;
        }

        private static async Task<bool> UpdateCachedShopItems(IEnumerable<OrderDigest> digests)
        {
            var selectedDigests = digests
                .Where(orderDigest => !CachedShopItems.ContainsKey(orderDigest.OrderId)).ToList();
            var tuples = selectedDigests
                .Select(e => (Address: Addresses.GetItemAddress(e.TradableId), OrderDigest: e))
                .ToArray();
            var itemAddresses = tuples.Select(tuple => tuple.Address).Distinct();
            var itemValues = await Game.Game.instance.Agent.GetStateBulk(itemAddresses);
            foreach (var (address, orderDigest) in tuples)
            {
                if (!itemValues.ContainsKey(address))
                {
                    Debug.LogWarning(
                        $"[{nameof(ReactiveShopState)}] Not found address: {address.ToHex()}");
                    continue;
                }

                var itemValue = itemValues[address];
                if (!(itemValue is Dictionary dictionary))
                {
                    Debug.LogWarning(
                        $"[{nameof(ReactiveShopState)}] {nameof(itemValue)} cannot cast to {typeof(Bencodex.Types.Dictionary).FullName}");
                    continue;
                }

                var itemBase = ItemFactory.Deserialize(dictionary);
                switch (itemBase)
                {
                    case TradableMaterial tm:
                        tm.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                        break;
                    case ItemUsable iu:
                        iu.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                        break;
                    case Costume c:
                        c.RequiredBlockIndex = orderDigest.ExpiredBlockIndex;
                        break;
                }

                CachedShopItems.TryAdd(orderDigest.OrderId, itemBase);
            }

            return true;
        }
    }
}
