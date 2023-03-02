using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("buy")]
    public class Buy0 : GameAction, IBuy0, IBuyV1
    {
        public Address buyerAvatarAddress { get; set; }
        public Address sellerAgentAddress { get; set; }
        public Address sellerAvatarAddress { get; set; }
        public Guid productId { get; set; }
        public Buy7.BuyerResult buyerResult;
        public Buy7.SellerResult sellerResult;

        Address IBuyV1.BuyerAvatarAddress => buyerAvatarAddress;
        Address IBuyV1.SellerAgentAddress => sellerAgentAddress;
        Address IBuyV1.SellerAvatarAddress => sellerAvatarAddress;
        Guid IBuyV1.ProductId => productId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.Serialize(),
            ["sellerAgentAddress"] = sellerAgentAddress.Serialize(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            sellerAgentAddress = plainValue["sellerAgentAddress"].ToAddress();
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states
                    .SetState(buyerAvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged)
                    .MarkBalanceChanged(
                        GoldCurrencyMock,
                        ctx.Signer,
                        sellerAgentAddress,
                        GoldCurrencyState.Address);
                return states.SetState(ShopState.Address, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress, sellerAvatarAddress);

            if (ctx.Signer.Equals(sellerAgentAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{Addresses}Buy exec started", addressesHex);

            if (!states.TryGetAvatarState(ctx.Signer, buyerAvatarAddress, out var buyerAvatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the buyer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Buyer AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!buyerAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                buyerAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary shopStateDict))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the shop state was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose(
                "{AddressesHex}Execute Buy; buyer: {Buyer} seller: {Seller}",
                addressesHex,
                buyerAvatarAddress,
                sellerAvatarAddress);
            // 상점에서 구매할 아이템을 찾는다.
            Dictionary products = (Dictionary)shopStateDict["products"];

            IKey productIdSerialized = (IKey)productId.Serialize();
            if (!products.ContainsKey(productIdSerialized))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the shop item ({productId}) was failed to get from the shop."
                );
            }

            ShopItem shopItem = new ShopItem((Dictionary)products[productIdSerialized]);
            if (!shopItem.SellerAgentAddress.Equals(sellerAgentAddress))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the shop item ({productId}) of seller ({shopItem.SellerAgentAddress}) is different from ({sellerAgentAddress})."
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the seller agent/avatar was failed to load from {sellerAgentAddress}/{sellerAvatarAddress}."
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            // 돈은 있냐?
            FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
            if (buyerBalance < shopItem.Price)
            {
                throw new InsufficientBalanceException(
                    $"{addressesHex}Aborted as the buyer ({ctx.Signer}) has no sufficient gold: {buyerBalance} < {shopItem.Price}",
                    ctx.Signer,
                    buyerBalance
                );
            }

            var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
            var taxedPrice = shopItem.Price - tax;

            // 세금을 송금한다.
            states = states.TransferAsset(
                context.Signer,
                GoldCurrencyState.Address,
                tax);

            // 구매자의 돈을 판매자에게 송금한다.
            states = states.TransferAsset(
                context.Signer,
                sellerAgentAddress,
                taxedPrice
            );

            products = (Dictionary)products.Remove(productIdSerialized);
            shopStateDict = shopStateDict.SetItem("products", products);

            // 구매자, 판매자에게 결과 메일 전송
            buyerResult = new Buy7.BuyerResult
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable
            };
            var buyerMail = new BuyerMail(buyerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
            buyerResult.id = buyerMail.id;

            sellerResult = new Buy7.SellerResult
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                gold = taxedPrice
            };
            var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                ctx.BlockIndex);
            sellerResult.id = sellerMail.id;

            buyerAvatarState.Update2(buyerMail);
            buyerAvatarState.UpdateFromAddItem2(buyerResult.itemUsable, false);
            sellerAvatarState.Update2(sellerMail);

            // 퀘스트 업데이트
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = ctx.BlockIndex;
            sellerAvatarState.blockIndex = ctx.BlockIndex;

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            buyerAvatarState.UpdateQuestRewards2(materialSheet);
            sellerAvatarState.UpdateQuestRewards2(materialSheet);

            //Avoid InvalidBlockStateRootHashException to 50000 index.
            if (sellerAvatarState.questList.Any(q => q.Complete && !q.IsPaidInAction))
            {
                var prevIds = sellerAvatarState.questList.completedQuestIds;
                sellerAvatarState.UpdateQuestRewards(materialSheet);
                sellerAvatarState.questList.completedQuestIds = prevIds;
            }
            if (context.BlockIndex != 4742 && buyerAvatarState.questList.Any(q => q.Complete && !q.IsPaidInAction))
            {
                var prevIds = buyerAvatarState.questList.completedQuestIds;
                buyerAvatarState.UpdateQuestRewards(materialSheet);
                buyerAvatarState.questList.completedQuestIds = prevIds;
            }

            states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
