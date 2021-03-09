using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
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
    [ActionType("buy_multiple")]
    public class BuyMultiple : GameAction
    {
        public Address buyerAvatarAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public IEnumerable<Guid> productIds;
        public BuyerResult buyerResult;
        public SellerResult sellerResult;

        [Serializable]
        public class BuyerResult
        {
            public IEnumerable<Buy.BuyerResult> buyerResults;

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized)
            {
                buyerResults = serialized["buyerResults"].ToList(StateExtensions.ToBuyerResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "buyerResults"] = buyerResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerResult
        {
            public IEnumerable<Buy.SellerResult> sellerResults;

            public SellerResult()
            {
            }

            public SellerResult(Bencodex.Types.Dictionary serialized)
            {
                sellerResults = serialized["sellerResults"].ToList(StateExtensions.ToSellerResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "sellerResults"] = sellerResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.Serialize(),
            ["sellerAgentAddress"] = sellerAgentAddress.Serialize(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productIds"] = productIds
                .OrderBy(i => i)
                .Select(g => g.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            sellerAgentAddress = plainValue["sellerAgentAddress"].ToAddress();
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productIds = plainValue["productIds"].ToList(StateExtensions.ToGuid);
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

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress, sellerAvatarAddress);

            if (ctx.Signer.Equals(sellerAgentAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{Addresses}BuyMultiple exec started", addressesHex);

            if (!states.TryGetAvatarState(ctx.Signer, buyerAvatarAddress, out var buyerAvatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the buyer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}BuyMultiple Get Buyer AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
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
            Log.Verbose("{AddressesHex}BuyMultiple Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose(
                "{AddressesHex}Execute BuyMultiple; buyer: {Buyer} seller: {Seller}",
                addressesHex,
                buyerAvatarAddress,
                sellerAvatarAddress);

            if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the seller agent/avatar was failed to load from {sellerAgentAddress}/{sellerAvatarAddress}."
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}BuyMultiple Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);

            var shopItems = new List<ShopItem>();

            // 상점에서 구매할 아이템을 찾는다.
            Dictionary products = (Dictionary)shopStateDict["products"];

            buyerResult = new BuyerResult();
            sellerResult = new SellerResult();
            var buyerResults = new List<Buy.BuyerResult>();
            var sellerResults = new List<Buy.SellerResult>();

            foreach (var productId in productIds)
            {
                sw.Restart();

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
                Log.Verbose("{AddressesHex}BuyMultiple Get Item: {Elapsed}", addressesHex, sw.Elapsed);

                shopItems.Add(shopItem);

                // 돈은 있냐?
                FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
                if (buyerBalance < shopItem.Price)
                {
                    throw new InsufficientBalanceException(
                        ctx.Signer,
                        buyerBalance,
                        $"{addressesHex}Aborted as the buyer ({ctx.Signer}) has no sufficient gold: {buyerBalance} < {shopItem.Price}"
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

                var buyerResultToAdd = new Buy.BuyerResult
                {
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume
                };
                var buyerMail = new BuyerMail(buyerResultToAdd, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
                buyerResultToAdd.id = buyerMail.id;
                buyerResults.Add(buyerResultToAdd);

                var sellerResultToAdd = new Buy.SellerResult
                {
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    gold = taxedPrice
                };
                var sellerMail = new SellerMail(sellerResultToAdd, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                    ctx.BlockIndex);
                sellerResultToAdd.id = sellerMail.id;
                sellerResults.Add(sellerResultToAdd);

                buyerAvatarState.UpdateV3(buyerMail);
                if (buyerResultToAdd.itemUsable != null)
                {
                    buyerAvatarState.UpdateFromAddItem(buyerResultToAdd.itemUsable, false);
                }

                if (buyerResultToAdd.costume != null)
                {
                    buyerAvatarState.UpdateFromAddCostume(buyerResultToAdd.costume, false);
                }
                sellerAvatarState.UpdateV3(sellerMail);

                // 퀘스트 업데이트
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);
            }

            buyerResult.buyerResults = buyerResults;
            sellerResult.sellerResults = sellerResults;

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = ctx.BlockIndex;
            sellerAvatarState.blockIndex = ctx.BlockIndex;

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            buyerAvatarState.UpdateQuestRewards(materialSheet);
            sellerAvatarState.UpdateQuestRewards(materialSheet);

            sw.Restart();
            states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}BuyMultiple Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}BuyMultiple Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            states = states.SetState(ShopState.Address, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}BuyMultiple Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex}BuyMultiple Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
