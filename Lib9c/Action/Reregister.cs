using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("reregister")]
    public class Reregister : GameAction
    {
        public Guid orderId;
        public Guid reregisterOrderId;
        public Guid tradableId;
        public Address sellerAvatarAddress;
        public ItemSubType itemSubType;
        public FungibleAssetValue price;
        public int count;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [OrderIdKey] = orderId.Serialize(),
                [ReregisterOrderIdKey] = reregisterOrderId.Serialize(),
                [ItemIdKey] = tradableId.Serialize(),
                [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [ItemSubTypeKey] = itemSubType.Serialize(),
                [PriceKey] = price.Serialize(),
                [ItemCountKey] = count.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            orderId = plainValue[OrderIdKey].ToGuid();
            reregisterOrderId = plainValue[ReregisterOrderIdKey].ToGuid();
            tradableId = plainValue[ItemIdKey].ToGuid();
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
            price = plainValue[PriceKey].ToFungibleAssetValue();
            count = plainValue[ItemCountKey].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
            var shopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            var reregisterShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, reregisterOrderId);
            var orderAddress = Order.DeriveAddress(orderId);
            var reregisterOrderAddress = Order.DeriveAddress(reregisterOrderId);
            var itemAddress = Addresses.GetItemAddress(tradableId);
            var orderReceiptAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
            if (context.Rehearsal)
            {
                return states
                    .SetState(context.Signer, MarkChanged)
                    .SetState(itemAddress, MarkChanged)
                    .SetState(shopAddress, MarkChanged)
                    .SetState(reregisterShopAddress, MarkChanged)
                    .SetState(orderAddress, MarkChanged)
                    .SetState(reregisterOrderAddress, MarkChanged)
                    .SetState(orderReceiptAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged);
            }

            // common
            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex} Reregister exec started", addressesHex);

            if (price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex} Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAvatarStateV2(context.Signer, sellerAvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();

            Log.Verbose("{AddressesHex} Sell Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!avatarState.worldInformation.IsStageCleared(
                GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }
            sw.Stop();

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            // for sell cancel
            Log.Verbose("{AddressesHex} Reregister IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!states.TryGetState(shopAddress, out BxDictionary shopStateDict))
            {
                throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shopAddress}).");
            }
            sw.Stop();

            Log.Verbose("{AddressesHex} Reregister Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!states.TryGetState(Order.DeriveAddress(orderId), out Dictionary orderDict))
            {
                throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(Order)}({Order.DeriveAddress(orderId)}).");
            }

            var orderOnSale = OrderFactory.Deserialize(orderDict);
            orderOnSale.ValidateCancelOrder(avatarState, tradableId);
            var itemOnSale = orderOnSale.Cancel(avatarState, context.BlockIndex);
            var shardedShopState = new ShardedShopStateV2(shopStateDict);
            shardedShopState.Remove(orderOnSale, context.BlockIndex);
            if (!states.TryGetState(orderReceiptAddress, out Dictionary rawList))
            {
                throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(OrderDigest)}({orderReceiptAddress}).");
            }
            var digestList = new OrderDigestListState(rawList);
            digestList.Remove(orderOnSale.OrderId);
            states = states.SetState(itemAddress, itemOnSale.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(shopAddress, shardedShopState.Serialize())
                .SetState(orderReceiptAddress, digestList.Serialize());
            sw.Stop();

            // for reregister
            var reregisterShopState = states.TryGetState(reregisterShopAddress, out Dictionary serializedState)
                ? new ShardedShopStateV2(serializedState)
                : new ShardedShopStateV2(reregisterShopAddress);

            Log.Verbose("{AddressesHex} Reregister Get ShardedShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            var newOrder = OrderFactory.Create(context.Signer, sellerAvatarAddress, reregisterOrderId, price, tradableId,
                context.BlockIndex, itemSubType, count);
            newOrder.Validate(avatarState, count);

            var tradableItem = newOrder.Sell(avatarState);
            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            var orderDigest = newOrder.Digest(avatarState, costumeStatSheet);
            reregisterShopState.Add(orderDigest, context.BlockIndex);

            var reregisterDigestList = states.TryGetState(orderReceiptAddress, out Dictionary receiptDict)
                ? new OrderDigestListState(receiptDict)
                : new OrderDigestListState(orderReceiptAddress);
            reregisterDigestList.Add(orderDigest);
            states = states.SetState(orderReceiptAddress, reregisterDigestList.Serialize());
            states = states.SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(sellerAvatarAddress, avatarState.SerializeV2());
            sw.Stop();

            Log.Verbose("{AddressesHex} Reregister Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            states = states
                .SetState(itemAddress, tradableItem.Serialize())
                .SetState(orderAddress, newOrder.Serialize())
                .SetState(reregisterShopAddress, reregisterShopState.Serialize());
            sw.Stop();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex} Reregister Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex} Reregister Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
