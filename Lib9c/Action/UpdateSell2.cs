using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/1022
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100270ObsoleteIndex)]
    [ActionType("update_sell2")]
    public class UpdateSell2 : GameAction, IUpdateSellV1
    {
        public Guid orderId;
        public Guid updateSellOrderId;
        public Guid tradableId;
        public Address sellerAvatarAddress;
        public ItemSubType itemSubType;
        public FungibleAssetValue price;
        public int count;

        Guid IUpdateSellV1.OrderId => orderId;
        Guid IUpdateSellV1.UpdateSellOrderId => updateSellOrderId;
        Guid IUpdateSellV1.TradableId => tradableId;
        Address IUpdateSellV1.SellerAvatarAddress => sellerAvatarAddress;
        string IUpdateSellV1.ItemSubType => itemSubType.ToString();
        FungibleAssetValue IUpdateSellV1.Price => price;
        int IUpdateSellV1.Count => count;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [OrderIdKey] = orderId.Serialize(),
                [updateSellOrderIdKey] = updateSellOrderId.Serialize(),
                [ItemIdKey] = tradableId.Serialize(),
                [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [ItemSubTypeKey] = itemSubType.Serialize(),
                [PriceKey] = price.Serialize(),
                [ItemCountKey] = count.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            orderId = plainValue[OrderIdKey].ToGuid();
            updateSellOrderId = plainValue[updateSellOrderIdKey].ToGuid();
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
            var updateSellShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, updateSellOrderId);
            var updateSellOrderAddress = Order.DeriveAddress(updateSellOrderId);
            var itemAddress = Addresses.GetItemAddress(tradableId);
            var digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
            if (context.Rehearsal)
            {
                return states
                    .SetState(context.Signer, MarkChanged)
                    .SetState(itemAddress, MarkChanged)
                    .SetState(digestListAddress, MarkChanged)
                    .SetState(shopAddress, MarkChanged)
                    .SetState(updateSellShopAddress, MarkChanged)
                    .SetState(updateSellOrderAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100270ObsoleteIndex, context);

            // common
            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex} updateSell exec started", addressesHex);

            if (price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex} Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAvatarStateV2(context.Signer, sellerAvatarAddress, out var avatarState, out _))
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

            if (!states.TryGetState(digestListAddress, out Dictionary rawList))
            {
                throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(OrderDigest)}({digestListAddress}).");
            }
            var digestList = new OrderDigestListState(rawList);

            // migration method
            avatarState.inventory.UnlockInvalidSlot(digestList, context.Signer, sellerAvatarAddress);
            avatarState.inventory.ReconfigureFungibleItem(digestList, tradableId);
            avatarState.inventory.LockByReferringToDigestList(digestList, tradableId, context.BlockIndex);
            //

            // for sell cancel
            Log.Verbose("{AddressesHex} UpdateSell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!states.TryGetState(shopAddress, out BxDictionary shopStateDict))
            {
                throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shopAddress}).");
            }
            sw.Stop();

            Log.Verbose("{AddressesHex} UpdateSell Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!states.TryGetState(Order.DeriveAddress(orderId), out Dictionary orderDict))
            {
                throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(Order)}({Order.DeriveAddress(orderId)}).");
            }

            var orderOnSale = OrderFactory.Deserialize(orderDict);
            orderOnSale.ValidateCancelOrder(avatarState, tradableId);
            var itemOnSale = orderOnSale.Cancel(avatarState, context.BlockIndex);
            if (context.BlockIndex < orderOnSale.ExpiredBlockIndex)
            {
                var shardedShopState = new ShardedShopStateV2(shopStateDict);
                shardedShopState.Remove(orderOnSale, context.BlockIndex);
                states = states.SetState(shopAddress, shardedShopState.Serialize());
            }

            digestList.Remove(orderOnSale.OrderId);
            states = states.SetState(itemAddress, itemOnSale.Serialize())
                .SetState(digestListAddress, digestList.Serialize());
            sw.Stop();

            var expirationMail = avatarState.mailBox.OfType<OrderExpirationMail>()
                            .FirstOrDefault(m => m.OrderId.Equals(orderId));
            if (!(expirationMail is null))
            {
                avatarState.mailBox.Remove(expirationMail);
            }

            // for updateSell
            var updateSellShopState = states.TryGetState(updateSellShopAddress, out Dictionary serializedState)
                ? new ShardedShopStateV2(serializedState)
                : new ShardedShopStateV2(updateSellShopAddress);

            Log.Verbose("{AddressesHex} UpdateSell Get ShardedShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            var newOrder = OrderFactory.Create(context.Signer, sellerAvatarAddress, updateSellOrderId, price, tradableId,
                context.BlockIndex, itemSubType, count);
            newOrder.Validate(avatarState, count);

            var tradableItem = newOrder.Sell4(avatarState);
            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            var orderDigest = newOrder.Digest(avatarState, costumeStatSheet);
            updateSellShopState.Add(orderDigest, context.BlockIndex);

            digestList.Add(orderDigest);
            states = states.SetState(digestListAddress, digestList.Serialize());
            states = states.SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(sellerAvatarAddress, avatarState.SerializeV2());
            sw.Stop();

            Log.Verbose("{AddressesHex} UpdateSell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            states = states
                .SetState(itemAddress, tradableItem.Serialize())
                .SetState(updateSellOrderAddress, newOrder.Serialize())
                .SetState(updateSellShopAddress, updateSellShopState.Serialize());
            sw.Stop();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex} UpdateSell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex} UpdateSell Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
