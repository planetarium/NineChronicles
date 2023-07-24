using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/609
    /// Updated at https://github.com/planetarium/lib9c/pull/620
    /// Updated at https://github.com/planetarium/lib9c/pull/861
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("sell_cancellation9")]
    public class SellCancellation : GameAction, ISellCancellationV3
    {
        public Guid orderId;
        public Guid tradableId;
        public Address sellerAvatarAddress;
        public ItemSubType itemSubType;

        Guid ISellCancellationV3.OrderId => orderId;
        Guid ISellCancellationV3.TradableId => tradableId;
        Address ISellCancellationV3.SellerAvatarAddress => sellerAvatarAddress;
        string ISellCancellationV3.ItemSubType => itemSubType.ToString();

        [Serializable]
        public class Result : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;

            protected override string TypeId => "sellCancellation.result";

            public Result()
            {
            }

            public Result(BxDictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((BxDictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new BxDictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize()
                }.Union((BxDictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [ProductIdKey] = orderId.Serialize(),
            [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
            [ItemSubTypeKey] = itemSubType.Serialize(),
            [TradableIdKey] = tradableId.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            orderId = plainValue[ProductIdKey].ToGuid();
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
            if (plainValue.ContainsKey(TradableIdKey))
            {
                tradableId = plainValue[TradableIdKey].ToGuid();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            var inventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
            var digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
            var itemAddress = Addresses.GetItemAddress(tradableId);
            if (context.Rehearsal)
            {
                states = states.SetState(shardedShopAddress, MarkChanged);
                return states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(digestListAddress, MarkChanged)
                    .SetState(itemAddress, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            if (!states.TryGetAvatarStateV2(context.Signer, sellerAvatarAddress, out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the seller failed to load.");
            }

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            if (!states.TryGetState(Order.DeriveAddress(orderId), out Dictionary orderDict))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}failed to load {nameof(Order)}({Order.DeriveAddress(orderId)}).");
            }

            Order order = OrderFactory.Deserialize(orderDict);
            if (tradableId != order.TradableId || itemSubType != order.ItemSubType)
            {
                return CancelV2(context, states, avatarState, addressesHex, order, tradableId, itemSubType);
            }

            return Cancel(context, states, avatarState, addressesHex, order);
        }

        public static IAccountStateDelta CancelV2(IActionContext context, IAccountStateDelta states, AvatarState avatarState, string addressesHex, Order order, Guid tradableId, ItemSubType itemSubType)
        {
            var orderTradableId = tradableId;
            var avatarAddress = avatarState.address;
            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, order.OrderId);
            Address digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            Address itemAddress = Addresses.GetItemAddress(orderTradableId);
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            Address worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            Address questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell Cancel exec started", addressesHex);

            if (!states.TryGetState(shardedShopAddress, out BxDictionary shopStateDict))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shardedShopAddress}).");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            if (!states.TryGetState(digestListAddress, out Dictionary rawList))
            {
                throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(OrderDigest)}({digestListAddress}).");
            }

            var digestList = new OrderDigestListState(rawList);

            // migration method
            avatarState.inventory.UnlockInvalidSlot(digestList, context.Signer, avatarAddress);
            avatarState.inventory.ReconfigureFungibleItem(digestList, orderTradableId);
            avatarState.inventory.LockByReferringToDigestList(digestList, orderTradableId, context.BlockIndex);
            //

            digestList.Remove(order.OrderId);

            order.ValidateCancelOrder(avatarState, orderTradableId);
            var sellItem = order.Cancel(avatarState, context.BlockIndex);
            if (context.BlockIndex < order.ExpiredBlockIndex)
            {
                var shardedShopState = new ShardedShopStateV2(shopStateDict);
                shardedShopState.Remove(order, context.BlockIndex);
                states = states.SetState(shardedShopAddress, shardedShopState.Serialize());
            }

            var expirationMail = avatarState.mailBox.OfType<OrderExpirationMail>()
                .FirstOrDefault(m => m.OrderId.Equals(order.OrderId));
            if (!(expirationMail is null))
            {
                avatarState.mailBox.Remove(expirationMail);
            }

            var mail = new CancelOrderMail(
                context.BlockIndex,
                order.OrderId,
                context.BlockIndex,
                order.OrderId
            );
            avatarState.Update(mail);

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(itemAddress, sellItem.Serialize())
                .SetState(digestListAddress, digestList.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Cancel Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug("{AddressesHex}Sell Cancel Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }

        public static IAccountStateDelta Cancel(IActionContext context, IAccountStateDelta states, AvatarState avatarState, string addressesHex, Order order)
        {
            var orderTradableId = order.TradableId;
            var avatarAddress = avatarState.address;
            Address shardedShopAddress = ShardedShopStateV2.DeriveAddress(order.ItemSubType, order.OrderId);
            Address digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            Address itemAddress = Addresses.GetItemAddress(orderTradableId);
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            Address worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            Address questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell Cancel exec started", addressesHex);

            if (!states.TryGetState(shardedShopAddress, out BxDictionary shopStateDict))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shardedShopAddress}).");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            if (!states.TryGetState(digestListAddress, out Dictionary rawList))
            {
                throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(OrderDigest)}({digestListAddress}).");
            }

            var digestList = new OrderDigestListState(rawList);

            // migration method
            avatarState.inventory.UnlockInvalidSlot(digestList, context.Signer, avatarAddress);
            avatarState.inventory.ReconfigureFungibleItem(digestList, orderTradableId);
            avatarState.inventory.LockByReferringToDigestList(digestList, orderTradableId, context.BlockIndex);
            //

            digestList.Remove(order.OrderId);

            order.ValidateCancelOrder(avatarState, orderTradableId);
            var sellItem = order.Cancel(avatarState, context.BlockIndex);
            if (context.BlockIndex < order.ExpiredBlockIndex)
            {
                var shardedShopState = new ShardedShopStateV2(shopStateDict);
                shardedShopState.Remove(order, context.BlockIndex);
                states = states.SetState(shardedShopAddress, shardedShopState.Serialize());
            }

            var expirationMail = avatarState.mailBox.OfType<OrderExpirationMail>()
                .FirstOrDefault(m => m.OrderId.Equals(order.OrderId));
            if (!(expirationMail is null))
            {
                avatarState.mailBox.Remove(expirationMail);
            }

            var mail = new CancelOrderMail(
                context.BlockIndex,
                order.OrderId,
                context.BlockIndex,
                order.OrderId
            );
            avatarState.Update(mail);

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(itemAddress, sellItem.Serialize())
                .SetState(digestListAddress, digestList.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Cancel Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug("{AddressesHex}Sell Cancel Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }
    }
}
