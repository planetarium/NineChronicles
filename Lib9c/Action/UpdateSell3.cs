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
using Nekoyume.Battle;
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
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1022
    /// Updated at https://github.com/planetarium/lib9c/pull/1022
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100320ObsoleteIndex)]
    [ActionType("update_sell3")]
    public class UpdateSell3 : GameAction, IUpdateSellV2
    {
        public Address sellerAvatarAddress;
        public IEnumerable<UpdateSellInfo> updateSellInfos;

        Address IUpdateSellV2.SellerAvatarAddress => sellerAvatarAddress;
        IEnumerable<IValue> IUpdateSellV2.UpdateSellInfos =>
            updateSellInfos.Select(x => x.Serialize());

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [UpdateSellInfoKey] = updateSellInfos.Select(info => info.Serialize()).Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            updateSellInfos = plainValue[UpdateSellInfoKey]
                .ToEnumerable(info => new UpdateSellInfo((List)info));
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
            var digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
            if (context.Rehearsal)
            {
                return states;
            }

            CheckObsolete(ActionObsoleteConfig.V100320ObsoleteIndex, context);

            // common
            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex} updateSell exec started", addressesHex);

            if (!updateSellInfos.Any())
            {
                throw new ListEmptyException($"{addressesHex} List - UpdateSell infos was empty.");
            }
            if (!states.TryGetAvatarStateV2(context.Signer, sellerAvatarAddress, out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex} Sell Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }
            sw.Stop();
            Log.Verbose("{AddressesHex} UpdateSell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();

            if (!states.TryGetState(digestListAddress, out Dictionary rawList))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} failed to load {nameof(OrderDigest)}({digestListAddress}).");
            }
            var digestList = new OrderDigestListState(rawList);

            foreach (var updateSellInfo in updateSellInfos)
            {
                if (updateSellInfo.price.Sign < 0)
                {
                    throw new InvalidPriceException($"{addressesHex} Aborted as the price is less than zero: {updateSellInfo.price}.");
                }

                var shopAddress = ShardedShopStateV2.DeriveAddress(updateSellInfo.itemSubType, updateSellInfo.orderId);
                var updateSellShopAddress = ShardedShopStateV2.DeriveAddress(updateSellInfo.itemSubType, updateSellInfo.updateSellOrderId);
                var updateSellOrderAddress = Order.DeriveAddress(updateSellInfo.updateSellOrderId);
                var itemAddress = Addresses.GetItemAddress(updateSellInfo.tradableId);

                // migration method
                avatarState.inventory.UnlockInvalidSlot(digestList, context.Signer, sellerAvatarAddress);
                avatarState.inventory.ReconfigureFungibleItem(digestList, updateSellInfo.tradableId);
                avatarState.inventory.LockByReferringToDigestList(digestList, updateSellInfo.tradableId,
                    context.BlockIndex);

                // for sell cancel
                sw.Restart();
                if (!states.TryGetState(shopAddress, out BxDictionary shopStateDict))
                {
                    throw new FailedLoadStateException($"{addressesHex}failed to load {nameof(ShardedShopStateV2)}({shopAddress}).");
                }

                sw.Stop();
                Log.Verbose("{AddressesHex} UpdateSell Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                if (!states.TryGetState(Order.DeriveAddress(updateSellInfo.orderId), out Dictionary orderDict))
                {
                    throw new FailedLoadStateException($"{addressesHex} failed to load {nameof(Order)}({Order.DeriveAddress(updateSellInfo.orderId)}).");
                }

                var orderOnSale = OrderFactory.Deserialize(orderDict);
                orderOnSale.ValidateCancelOrder(avatarState, updateSellInfo.tradableId);
                orderOnSale.Cancel(avatarState, context.BlockIndex);
                if (context.BlockIndex < orderOnSale.ExpiredBlockIndex)
                {
                    var shardedShopState = new ShardedShopStateV2(shopStateDict);
                    shardedShopState.Remove(orderOnSale, context.BlockIndex);
                    states = states.SetState(shopAddress, shardedShopState.Serialize());
                }

                digestList.Remove(orderOnSale.OrderId);
                sw.Stop();

                var expirationMail = avatarState.mailBox.OfType<OrderExpirationMail>()
                    .FirstOrDefault(m => m.OrderId.Equals(updateSellInfo.orderId));
                if (!(expirationMail is null))
                {
                    avatarState.mailBox.Remove(expirationMail);
                }

                // for updateSell
                var updateSellShopState =
                    states.TryGetState(updateSellShopAddress, out Dictionary serializedState)
                        ? new ShardedShopStateV2(serializedState)
                        : new ShardedShopStateV2(updateSellShopAddress);

                Log.Verbose("{AddressesHex} UpdateSell Get ShardedShopState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                var newOrder = OrderFactory.Create(
                    context.Signer,
                    sellerAvatarAddress,
                    updateSellInfo.updateSellOrderId,
                    updateSellInfo.price,
                    updateSellInfo.tradableId,
                    context.BlockIndex,
                    updateSellInfo.itemSubType,
                    updateSellInfo.count
                );

                newOrder.Validate(avatarState, updateSellInfo.count);

                var tradableItem = newOrder.Sell4(avatarState);
                var orderDigest = newOrder.Digest(avatarState, costumeStatSheet);
                updateSellShopState.Add(orderDigest, context.BlockIndex);

                digestList.Add(orderDigest);

                states = states
                    .SetState(itemAddress, tradableItem.Serialize())
                    .SetState(updateSellOrderAddress, newOrder.Serialize())
                    .SetState(updateSellShopAddress, updateSellShopState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex} UpdateSell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            sw.Restart();
            states = states.SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(sellerAvatarAddress, avatarState.SerializeV2())
                .SetState(digestListAddress, digestList.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex} UpdateSell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex} UpdateSell Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
