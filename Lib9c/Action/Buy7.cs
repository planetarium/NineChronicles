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
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("buy7")]
    public class Buy7 : GameAction, IBuy5
    {
        public const int TaxRate = 8;
        public const int ErrorCodeFailedLoadingState = 1;
        public const int ErrorCodeItemDoesNotExist = 2;
        public const int ErrorCodeShopItemExpired = 3;
        public const int ErrorCodeInsufficientBalance = 4;
        public const int ErrorCodeInvalidAddress = 5;
        public const int ErrorCodeInvalidPrice = 6;

        public Address buyerAvatarAddress { get; set; }
        public IEnumerable<PurchaseInfo0> purchaseInfos;
        IEnumerable<IPurchaseInfo> IBuy5.purchaseInfos => purchaseInfos.Cast<IPurchaseInfo>();
        public BuyerMultipleResult buyerMultipleResult;
        public SellerMultipleResult sellerMultipleResult;

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

        [Serializable]
        public class PurchaseResult : BuyerResult
        {
            public int errorCode = 0;
            public readonly Guid productId;

            public PurchaseResult(Guid shopProductId)
            {
                productId = shopProductId;
            }

            public PurchaseResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                errorCode = serialized[ErrorCodeKey].ToInteger();
                productId = serialized[ProductIdKey].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) ErrorCodeKey] = errorCode.Serialize(),
                    [(Text) ProductIdKey] = productId.Serialize(),
                }.Union((Bencodex.Types.Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class BuyerMultipleResult
        {
            public IEnumerable<PurchaseResult> purchaseResults;

            public BuyerMultipleResult()
            {
            }

            public BuyerMultipleResult(Bencodex.Types.Dictionary serialized)
            {
                purchaseResults = serialized[PurchaseResultsKey].ToList(StateExtensions.ToPurchaseResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) PurchaseResultsKey] = purchaseResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerMultipleResult
        {
            public IEnumerable<SellerResult> sellerResults;

            public SellerMultipleResult()
            {
            }

            public SellerMultipleResult(Bencodex.Types.Dictionary serialized)
            {
                sellerResults = serialized[SellerResultsKey].ToList(StateExtensions.ToSellerResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) SellerResultsKey] = sellerResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }



        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [BuyerAvatarAddressKey] = buyerAvatarAddress.Serialize(),
            [PurchaseInfosKey] = purchaseInfos
                .OrderBy(p => p.productId)
                .Select(p => p.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue[BuyerAvatarAddressKey].ToAddress();
            purchaseInfos = plainValue[PurchaseInfosKey].ToList(StateExtensions.ToPurchaseInfo);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    Address shardedShopAddress =
                        ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                    states = states
                        .SetState(shardedShopAddress, MarkChanged)
                        .SetState(purchaseInfo.sellerAvatarAddress, MarkChanged)
                        .MarkBalanceChanged(
                            GoldCurrencyMock,
                            ctx.Signer,
                            purchaseInfo.sellerAgentAddress,
                            GoldCurrencyState.Address);
                }
                return states
                    .SetState(buyerAvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(Addresses.Shop, MarkChanged);
            }

            CheckObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress);

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

            List<PurchaseResult> purchaseResults = new List<PurchaseResult>();
            List<SellerResult> sellerResults = new List<SellerResult>();
            MaterialItemSheet materialSheet = states.GetSheet<MaterialItemSheet>();
            buyerMultipleResult = new BuyerMultipleResult();
            sellerMultipleResult = new SellerMultipleResult();

            foreach (var purchaseInfo in purchaseInfos)
            {
                PurchaseResult purchaseResult = new PurchaseResult(purchaseInfo.productId);
                Address shardedShopAddress =
                    ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                Address sellerAgentAddress = purchaseInfo.sellerAgentAddress;
                Address sellerAvatarAddress = purchaseInfo.sellerAvatarAddress;
                Guid productId = purchaseInfo.productId;

                purchaseResults.Add(purchaseResult);

                if (purchaseInfo.sellerAgentAddress == ctx.Signer)
                {
                    purchaseResult.errorCode = ErrorCodeInvalidAddress;
                    continue;
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
                IValue sellerAgentSerialized = purchaseInfo.sellerAgentAddress.Serialize();
                IValue sellerAvatarSerialized = purchaseInfo.sellerAvatarAddress.Serialize();
                Dictionary productSerialized = products
                    .Select(p => (Dictionary) p)
                    .FirstOrDefault(p =>
                        p[LegacyProductIdKey].Equals(productIdSerialized) &&
                        p[LegacySellerAvatarAddressKey].Equals(sellerAvatarSerialized) &&
                        p[LegacySellerAgentAddressKey].Equals(sellerAgentSerialized));

                // Since Bencodex 0.4, Dictionary/List are reference types; so their default values
                // are not a empty container, but a null reference:
                productSerialized = productSerialized ?? Dictionary.Empty;

                bool fromLegacy = false;
                if (productSerialized.Equals(Dictionary.Empty))
                {
                    if (purchaseInfo.itemSubType == ItemSubType.Hourglass ||
                        purchaseInfo.itemSubType == ItemSubType.ApStone)
                    {
                        purchaseResult.errorCode = ErrorCodeItemDoesNotExist;
                        continue;
                    }
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
                            purchaseResult.errorCode = ErrorCodeItemDoesNotExist;
                            continue;
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
                    purchaseResult.errorCode = ErrorCodeItemDoesNotExist;
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                if (0 < shopItem.ExpiredBlockIndex && shopItem.ExpiredBlockIndex < context.BlockIndex)
                {
                    purchaseResult.errorCode = ErrorCodeShopItemExpired;
                    continue;
                }

                if (!shopItem.Price.Equals(purchaseInfo.price))
                {
                    purchaseResult.errorCode = ErrorCodeInvalidPrice;
                    continue;
                }

                if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
                {
                    purchaseResult.errorCode = ErrorCodeFailedLoadingState;
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                // Check Balance.
                FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
                if (buyerBalance < shopItem.Price)
                {
                    purchaseResult.errorCode = ErrorCodeInsufficientBalance;
                    continue;
                }

                // Check Seller inventory.
                ITradableItem tradableItem;
                int count = 1;
                if (!(shopItem.ItemUsable is null))
                {
                    tradableItem = shopItem.ItemUsable;
                }
                else if (!(shopItem.Costume is null))
                {
                    tradableItem = shopItem.Costume;
                }
                else
                {
                    tradableItem = shopItem.TradableFungibleItem;
                    count = shopItem.TradableFungibleItemCount;
                }

                if (!sellerAvatarState.inventory.RemoveTradableItem(tradableItem, count) && !fromLegacy)
                {
                    purchaseResult.errorCode = ErrorCodeItemDoesNotExist;
                    continue;
                }

                tradableItem.RequiredBlockIndex = context.BlockIndex;

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
                shopStateDict = shopStateDict.SetItem(ProductsKey, products);

                // Send result mail for buyer, seller.
                purchaseResult.shopItem = shopItem;
                purchaseResult.itemUsable = shopItem.ItemUsable;
                purchaseResult.costume = shopItem.Costume;
                purchaseResult.tradableFungibleItem = shopItem.TradableFungibleItem;
                purchaseResult.tradableFungibleItemCount = shopItem.TradableFungibleItemCount;
                var buyerMail = new BuyerMail(purchaseResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                    ctx.BlockIndex);
                purchaseResult.id = buyerMail.id;

                var sellerResult = new SellerResult
                {
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    tradableFungibleItem = shopItem.TradableFungibleItem,
                    tradableFungibleItemCount = shopItem.TradableFungibleItemCount,
                    gold = taxedPrice
                };
                var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                    ctx.BlockIndex);
                sellerResult.id = sellerMail.id;
                sellerResults.Add(sellerResult);

                buyerAvatarState.Update(buyerMail);
                if (purchaseResult.itemUsable != null)
                {
                    buyerAvatarState.UpdateFromAddItem2(purchaseResult.itemUsable, false);
                }

                if (purchaseResult.costume != null)
                {
                    buyerAvatarState.UpdateFromAddCostume(purchaseResult.costume, false);
                }

                if (purchaseResult.tradableFungibleItem is TradableMaterial material)
                {
                    buyerAvatarState.UpdateFromAddItem2(material, shopItem.TradableFungibleItemCount, false);
                }

                sellerAvatarState.Update(sellerMail);

                // Update quest.
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);

                sellerAvatarState.updatedAt = ctx.BlockIndex;
                sellerAvatarState.blockIndex = ctx.BlockIndex;

                buyerAvatarState.UpdateQuestRewards2(materialSheet);
                sellerAvatarState.UpdateQuestRewards2(materialSheet);

                states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(shardedShopAddress, shopStateDict);
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            buyerMultipleResult.purchaseResults = purchaseResults;
            sellerMultipleResult.sellerResults = sellerResults;

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
