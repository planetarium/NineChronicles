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
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("buy5")]
    public class Buy : GameAction
    {
        public const int TaxRate = 8;

        public Address buyerAvatarAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;
        public BuyerResult buyerResult;
        public SellerResult sellerResult;
        public ItemSubType itemSubType;

        [Serializable]
        public class BuyerResult : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;

            protected override string TypeId => "buy.buyerResult";

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerResult : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;
            public FungibleAssetValue gold;

            protected override string TypeId => "buy.sellerResult";

            public SellerResult()
            {
            }

            public SellerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
                gold = serialized["gold"].ToFungibleAssetValue();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.Serialize(),
            ["sellerAgentAddress"] = sellerAgentAddress.Serialize(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
            ["i"] = itemSubType.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            sellerAgentAddress = plainValue["sellerAgentAddress"].ToAddress();
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
            itemSubType = plainValue["i"].ToEnum<ItemSubType>();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
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
                return states
                    .SetState(Addresses.Shop, MarkChanged)
                    .SetState(shardedShopAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress, sellerAvatarAddress);

            if (ctx.Signer.Equals(sellerAgentAddress))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy exec started", addressesHex);

            if (!states.TryGetAvatarState(ctx.Signer, buyerAvatarAddress, out var buyerAvatarState))
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

            if (!states.TryGetState(shardedShopAddress, out Bencodex.Types.Dictionary shopStateDict))
            {
                ShardedShopState shardedShopState = new ShardedShopState(shardedShopAddress);
                shopStateDict = (Dictionary) shardedShopState.Serialize();
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose(
                "{AddressesHex}Execute Buy; buyer: {Buyer} seller: {Seller}",
                addressesHex,
                buyerAvatarAddress,
                sellerAvatarAddress);
            // Find product from ShardedShopState.
            List products = (List) shopStateDict[ProductsKey];
            IValue productIdSerialized = productId.Serialize();
            Dictionary productSerialized = products
                .Select(p => (Dictionary) p)
                .FirstOrDefault(p => p[ProductIdKey].Equals(productIdSerialized));

            bool fromLegacy = false;
            if (productSerialized.Equals(Dictionary.Empty))
            {
                // Backward compatibility.
                IValue rawShop = states.GetState(Addresses.Shop);
                if (!(rawShop is null))
                {
                    Dictionary legacyShopDict = (Dictionary) rawShop;
                    Dictionary legacyProducts = (Dictionary) legacyShopDict[LegacyProductsKey];
                    IKey productKey = (IKey) productId.Serialize();
                    // SoldOut
                    if (!legacyProducts.ContainsKey(productKey))
                    {
                        throw new ItemDoesNotExistException(
                            $"{addressesHex}Aborted as the shop item ({productId}) was failed to get from the legacy shop."
                        );
                    }

                    productSerialized = (Dictionary) legacyProducts[productKey];
                    legacyProducts = (Dictionary) legacyProducts.Remove(productKey);
                    legacyShopDict = legacyShopDict.SetItem(LegacyProductsKey, legacyProducts);
                    states = states.SetState(Addresses.Shop, legacyShopDict);
                    fromLegacy = true;
                }
            }

            ShopItem shopItem = new ShopItem(productSerialized);
            if (!shopItem.SellerAgentAddress.Equals(sellerAgentAddress))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the shop item ({productId}) of seller ({shopItem.SellerAgentAddress}) is different from ({sellerAgentAddress})."
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (0 < shopItem.ExpiredBlockIndex && shopItem.ExpiredBlockIndex < context.BlockIndex)
            {
                throw new ShopItemExpiredException(
                    $"{addressesHex}Aborted as the shop item ({productId}) already expired on # ({shopItem.ExpiredBlockIndex}).");
            }

            if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the seller agent/avatar was failed to load from {sellerAgentAddress}/{sellerAvatarAddress}."
                );
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            // Check Balance.
            FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
            if (buyerBalance < shopItem.Price)
            {
                throw new InsufficientBalanceException(
                    ctx.Signer,
                    buyerBalance,
                    $"{addressesHex}Aborted as the buyer ({ctx.Signer}) has no sufficient gold: {buyerBalance} < {shopItem.Price}"
                );
            }

            var tax = shopItem.Price.DivRem(100, out _) * TaxRate;
            var taxedPrice = shopItem.Price - tax;

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

            products = (List) products.Remove(productSerialized);
            shopStateDict = shopStateDict.SetItem(ProductsKey, new List<IValue>(products));

            INonFungibleItem nonFungibleItem = (INonFungibleItem) shopItem.ItemUsable ?? shopItem.Costume;
            if (!sellerAvatarState.inventory.RemoveNonFungibleItem(nonFungibleItem) && !fromLegacy)
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the shop item ({productId}) was failed to get from the legacy shop."
                );
            }

            nonFungibleItem.Update(context.BlockIndex);

            // Send result mail for buyer, seller.
            buyerResult = new BuyerResult
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                costume = shopItem.Costume
            };
            var buyerMail = new BuyerMail(buyerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                ctx.BlockIndex);
            buyerResult.id = buyerMail.id;

            sellerResult = new SellerResult
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                costume = shopItem.Costume,
                gold = taxedPrice
            };
            var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                ctx.BlockIndex);
            sellerResult.id = sellerMail.id;

            buyerAvatarState.UpdateV4(buyerMail, context.BlockIndex);
            if (buyerResult.itemUsable != null)
            {
                buyerAvatarState.UpdateFromAddItem(buyerResult.itemUsable, false);
            }

            if (buyerResult.costume != null)
            {
                buyerAvatarState.UpdateFromAddCostume(buyerResult.costume, false);
            }

            sellerAvatarState.UpdateV4(sellerMail, context.BlockIndex);

            // Update quest.
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = ctx.BlockIndex;
            sellerAvatarState.blockIndex = ctx.BlockIndex;

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            buyerAvatarState.UpdateQuestRewards(materialSheet);
            sellerAvatarState.UpdateQuestRewards(materialSheet);

            states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(shardedShopAddress, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
