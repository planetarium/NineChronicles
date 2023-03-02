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
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100220ObsoleteIndex)]
    [ActionType("buy10")]
    public class Buy10 : GameAction, IBuy5, IBuyV2
    {
        public const int TaxRate = 8;
        public const int ErrorCodeFailedLoadingState = 1;
        public const int ErrorCodeItemDoesNotExist = 2;
        public const int ErrorCodeShopItemExpired = 3;
        public const int ErrorCodeInsufficientBalance = 4;
        public const int ErrorCodeInvalidAddress = 5;
        public const int ErrorCodeInvalidPrice = 6;
        public const int ErrorCodeInvalidOrderId = 7;
        public const int ErrorCodeInvalidTradableId = 8;
        public const int ErrorCodeInvalidItemType = 9;
        public const int ErrorCodeDuplicateSell = 10;

        public Address buyerAvatarAddress { get; set; }
        public List<(Guid orderId, int errorCode)> errors = new List<(Guid orderId, int errorCode)>();
        public IEnumerable<PurchaseInfo> purchaseInfos;
        IEnumerable<IPurchaseInfo> IBuy5.purchaseInfos => purchaseInfos;

        Address IBuyV2.BuyerAvatarAddress => buyerAvatarAddress;
        IEnumerable<IValue> IBuyV2.PurchaseInfos => purchaseInfos.Select(x => x.Serialize());

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [BuyerAvatarAddressKey] = buyerAvatarAddress.Serialize(),
            [PurchaseInfosKey] = purchaseInfos
                .OrderBy(p => p.OrderId)
                .Select(p => p.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue[BuyerAvatarAddressKey].ToAddress();
            purchaseInfos = plainValue[PurchaseInfosKey].ToList(value => new PurchaseInfo((Dictionary)value));
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var buyerInventoryAddress = buyerAvatarAddress.Derive(LegacyInventoryKey);
            var buyerWorldInformationAddress = buyerAvatarAddress.Derive(LegacyWorldInformationKey);
            var buyerQuestListAddress = buyerAvatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    var sellerAvatarAddress = purchaseInfo.SellerAvatarAddress;
                    var sellerInventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
                    var sellerWorldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
                    var sellerQuestListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
                    Address shardedShopAddress =
                        ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
                    Address orderReceiptAddress = OrderReceipt.DeriveAddress(purchaseInfo.OrderId);
                    Address digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
                    states = states
                        .SetState(shardedShopAddress, MarkChanged)
                        .SetState(sellerAvatarAddress, MarkChanged)
                        .SetState(sellerInventoryAddress, MarkChanged)
                        .SetState(sellerWorldInformationAddress, MarkChanged)
                        .SetState(sellerQuestListAddress, MarkChanged)
                        .SetState(orderReceiptAddress, MarkChanged)
                        .SetState(digestListAddress, MarkChanged)
                        .MarkBalanceChanged(
                            GoldCurrencyMock,
                            ctx.Signer,
                            purchaseInfo.SellerAgentAddress,
                            GoldCurrencyState.Address);
                }

                return states
                    .SetState(buyerAvatarAddress, MarkChanged)
                    .SetState(buyerInventoryAddress, MarkChanged)
                    .SetState(buyerWorldInformationAddress, MarkChanged)
                    .SetState(buyerQuestListAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100220ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(ctx.Signer, buyerAvatarAddress, out var buyerAvatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the buyer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Buyer AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!buyerAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                buyerAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            MaterialItemSheet materialSheet = states.GetSheet<MaterialItemSheet>();

            foreach (var purchaseInfo in purchaseInfos)
            {
                Address shardedShopAddress =
                    ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
                Address sellerAgentAddress = purchaseInfo.SellerAgentAddress;
                Address sellerAvatarAddress = purchaseInfo.SellerAvatarAddress;
                Address sellerInventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
                var sellerWorldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
                Address sellerQuestListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
                Guid orderId = purchaseInfo.OrderId;
                Address orderAddress = Order.DeriveAddress(orderId);
                Address digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);

                if (purchaseInfo.SellerAgentAddress == ctx.Signer)
                {
                    errors.Add((orderId, ErrorCodeInvalidAddress));
                    continue;
                }

                if (!states.TryGetState(shardedShopAddress, out Bencodex.Types.Dictionary shopStateDict))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }

                if (!states.TryGetState(orderAddress, out Dictionary rawOrder))
                {
                    errors.Add((orderId, ErrorCodeInvalidOrderId));
                    continue;
                }

                Order order = OrderFactory.Deserialize(rawOrder);

                var shardedShopState = new ShardedShopStateV2(shopStateDict);

                try
                {
                    shardedShopState.Remove(order, context.BlockIndex);
                }
                catch (OrderIdDoesNotExistException)
                {
                    errors.Add((orderId, ErrorCodeInvalidOrderId));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                Log.Verbose(
                    "{AddressesHex}Execute Buy; buyer: {Buyer} seller: {Seller}",
                    addressesHex,
                    buyerAvatarAddress,
                    sellerAvatarAddress);


                if (!states.TryGetAvatarStateV2(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState, out _))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                if (!states.TryGetState(digestListAddress, out Dictionary rawDigestList))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }
                var digestList = new OrderDigestListState(rawDigestList);

                // migration method
                sellerAvatarState.inventory.UnlockInvalidSlot(digestList, sellerAgentAddress, sellerAvatarAddress);
                sellerAvatarState.inventory.ReconfigureFungibleItem(digestList, order.TradableId);
                sellerAvatarState.inventory.LockByReferringToDigestList(digestList, order.TradableId, context.BlockIndex);
                //

                digestList.Remove(orderId);

                var errorCode = order.ValidateTransfer(sellerAvatarState, purchaseInfo.TradableId, purchaseInfo.Price, context.BlockIndex);
                if (errorCode != 0)
                {
                    errors.Add((orderId, errorCode));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                // Check Balance.
                FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
                if (buyerBalance < order.Price)
                {
                    errors.Add((orderId, ErrorCodeInsufficientBalance));
                    continue;
                }

                OrderReceipt orderReceipt;
                try
                {
                    orderReceipt = order.Transfer(sellerAvatarState, buyerAvatarState, context.BlockIndex);
                }
                catch (ItemDoesNotExistException)
                {
                    errors.Add((orderId, ErrorCodeItemDoesNotExist));
                    continue;
                }

                Address orderReceiptAddress = OrderReceipt.DeriveAddress(orderId);
                if (!(states.GetState(orderReceiptAddress) is null))
                {
                    errors.Add((orderId, ErrorCodeDuplicateSell));
                    continue;
                }

                var expirationMail = sellerAvatarState.mailBox.OfType<OrderExpirationMail>()
                    .FirstOrDefault(m => m.OrderId.Equals(orderId));
                if (!(expirationMail is null))
                {
                    sellerAvatarState.mailBox.Remove(expirationMail);
                }

                var orderSellerMail = new OrderSellerMail(
                    context.BlockIndex,
                    orderId,
                    context.BlockIndex,
                    orderId
                );
                var orderBuyerMail = new OrderBuyerMail(
                    context.BlockIndex,
                    orderId,
                    context.BlockIndex,
                    orderId
                );

                buyerAvatarState.Update(orderBuyerMail);
                sellerAvatarState.Update(orderSellerMail);

                // // Update quest.
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, order.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, order.Price);

                sellerAvatarState.updatedAt = ctx.BlockIndex;
                sellerAvatarState.blockIndex = ctx.BlockIndex;

                buyerAvatarState.UpdateQuestRewards(materialSheet);
                sellerAvatarState.UpdateQuestRewards(materialSheet);

                FungibleAssetValue tax = order.GetTax();
                var taxedPrice = order.Price - tax;

                // Transfer tax.
                states = states.TransferAsset(
                    context.Signer,
                    GoldCurrencyState.Address,
                    tax);

                // Transfer seller.
                states = states.TransferAsset(
                    context.Signer,
                    sellerAgentAddress,
                    taxedPrice
                );

                states = states
                    .SetState(digestListAddress, digestList.Serialize())
                    .SetState(orderReceiptAddress, orderReceipt.Serialize())
                    .SetState(sellerInventoryAddress, sellerAvatarState.inventory.Serialize())
                    .SetState(sellerWorldInformationAddress, sellerAvatarState.worldInformation.Serialize())
                    .SetState(sellerQuestListAddress, sellerAvatarState.questList.Serialize())
                    .SetState(sellerAvatarAddress, sellerAvatarState.SerializeV2());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(shardedShopAddress, shardedShopState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;

            states = states
                .SetState(buyerInventoryAddress, buyerAvatarState.inventory.Serialize())
                .SetState(buyerWorldInformationAddress, buyerAvatarState.worldInformation.Serialize())
                .SetState(buyerQuestListAddress, buyerAvatarState.questList.Serialize())
                .SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
