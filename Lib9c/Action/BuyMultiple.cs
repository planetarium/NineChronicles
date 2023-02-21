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
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/331
    /// Updated at many pull requests
    /// Obsoleted at https://github.com/planetarium/lib9c/pull/487
    /// Updated at many pull requests
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("buy_multiple")]
    public class BuyMultiple : GameAction, IBuyMultipleV1
    {
        public Address buyerAvatarAddress;
        public IEnumerable<PurchaseInfo> purchaseInfos;
        public BuyerResult buyerResult;
        public SellerResult sellerResult;

        Address IBuyMultipleV1.BuyerAvatarAddress => buyerAvatarAddress;
        IEnumerable<IValue> IBuyMultipleV1.PurchaseInfos => purchaseInfos.Select(x => x.Serialize());

        public const int ERROR_CODE_FAILED_LOADING_STATE = 1;
        public const int ERROR_CODE_ITEM_DOES_NOT_EXIST = 2;
        public const int ERROR_CODE_SHOPITEM_EXPIRED = 3;
        public const int ERROR_CODE_INSUFFICIENT_BALANCE = 4;

        [Serializable]
        public class PurchaseInfo : IComparable<PurchaseInfo>, IComparable
        {
            public static bool operator >(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) > 0;

            public static bool operator <(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) < 0;

            public static bool operator >=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) >= 0;

            public static bool operator <=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) <= 0;

            public static bool operator ==(PurchaseInfo left, PurchaseInfo right) => left.Equals(right);
            public static bool operator !=(PurchaseInfo left, PurchaseInfo right) => !(left == right);

            protected bool Equals(PurchaseInfo other)
            {
                return productId.Equals(other.productId) && sellerAgentAddress.Equals(other.sellerAgentAddress) && sellerAvatarAddress.Equals(other.sellerAvatarAddress);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PurchaseInfo) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = productId.GetHashCode();
                    hashCode = (hashCode * 397) ^ sellerAgentAddress.GetHashCode();
                    hashCode = (hashCode * 397) ^ sellerAvatarAddress.GetHashCode();
                    return hashCode;
                }
            }

            public Guid productId;
            public Address sellerAgentAddress;
            public Address sellerAvatarAddress;

            public PurchaseInfo(Guid productId, Address sellerAgentAddress, Address sellerAvatarAddress)
            {
                this.productId = productId;
                this.sellerAgentAddress = sellerAgentAddress;
                this.sellerAvatarAddress = sellerAvatarAddress;
            }

            public PurchaseInfo(Bencodex.Types.Dictionary serialized)
            {
                productId = serialized["productId"].ToGuid();
                sellerAvatarAddress = serialized["sellerAvatarAddress"].ToAddress();
                sellerAgentAddress = serialized["sellerAgentAddress"].ToAddress();
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "productId"] = productId.Serialize(),
                    [(Text) "sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
                    [(Text) "sellerAgentAddress"] = sellerAgentAddress.Serialize(),
                });
#pragma warning restore LAA1002
            public int CompareTo(PurchaseInfo other)
            {
                return productId.CompareTo(other.productId);
            }

            public int CompareTo(object obj)
            {
                if (obj is PurchaseInfo other)
                {
                    return CompareTo(other);
                }

                throw new ArgumentException(nameof(obj));
            }
        }

        [Serializable]
        public class PurchaseResult : Buy7.BuyerResult
        {
            public int errorCode = 0;
            public Guid productId;

            public PurchaseResult(Guid shopProductId)
            {
                productId = shopProductId;
            }

            public PurchaseResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                errorCode = serialized["errorCode"].ToInteger();
                productId = serialized["productId"].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "errorCode"] = errorCode.Serialize(),
                    [(Text) "productId"] = productId.Serialize(),
                }.Union((Bencodex.Types.Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class BuyerResult
        {
            public IEnumerable<PurchaseResult> purchaseResults;

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized)
            {
                purchaseResults = serialized["purchaseResults"].ToList(StateExtensions.ToPurchaseResultLegacy);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "purchaseResults"] = purchaseResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerResult
        {
            public IEnumerable<Buy7.SellerResult> sellerResults;

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
            ["products"] = purchaseInfos
                .OrderBy(i => i)
                .Select(g => g.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            purchaseInfos = plainValue["products"].ToList(StateExtensions.ToPurchaseInfoLegacy);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;

            if (ctx.Rehearsal)
            {
                states = states
                    .SetState(buyerAvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged);

                foreach (var info in purchaseInfos)
                {
                    var sellerAgentAddress = info.sellerAgentAddress;
                    var sellerAvatarAddress = info.sellerAvatarAddress;

                    states = states.SetState(sellerAvatarAddress, MarkChanged)
                        .MarkBalanceChanged(
                            GoldCurrencyMock,
                            ctx.Signer,
                            sellerAgentAddress,
                            GoldCurrencyState.Address);
                }

                return states.SetState(ShopState.Address, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var availableInfos = purchaseInfos.Where(p => !(p is null));

            var sellerAgentAddresses = availableInfos.Select(p => p.sellerAgentAddress);
            var sellerAvatarAddresses = availableInfos.Select(p => p.sellerAvatarAddress);
            var avatarAddresses = sellerAvatarAddresses.Prepend(buyerAvatarAddress);
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddresses.ToArray());

            if (sellerAgentAddresses.Any(a => a.Equals(ctx.Signer)))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{Addresses}BuyMultiple exec started", addressesHex);

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

            var sellerAvatarAddressesString = string.Join(", ", sellerAvatarAddresses.Select(a => a.ToString()));

            Log.Verbose(
                "{AddressesHex}Execute BuyMultiple; buyer: {Buyer} sellers: {Seller}",
                addressesHex,
                buyerAvatarAddress,
                sellerAvatarAddressesString);

            // Get products in `ShopState`.
            Dictionary productDict = (Dictionary) shopStateDict["products"];

            buyerResult = new BuyerResult();
            sellerResult = new SellerResult();
            var purchaseResults = new List<PurchaseResult>();
            var sellerResults = new List<Buy7.SellerResult>();
            var materialSheet = states.GetSheet<MaterialItemSheet>();

            foreach (var productInfo in purchaseInfos)
            {
                if (productInfo is null)
                {
                    continue;
                }
                var productId = productInfo.productId;
                var purchaseResult = new PurchaseResult(productId);
                purchaseResults.Add(purchaseResult);


                IKey productIdSerialized = (IKey) productId.Serialize();
                if (!productDict.ContainsKey(productIdSerialized))
                {
                    purchaseResult.errorCode = ERROR_CODE_ITEM_DOES_NOT_EXIST;
                    continue;
                }

                ShopItem shopItem = new ShopItem((Dictionary) productDict[productIdSerialized]);
                purchaseResult.shopItem = shopItem;
                purchaseResult.itemUsable = shopItem.ItemUsable;
                purchaseResult.costume = shopItem.Costume;

                sw.Restart();

                var avatarAddress = productInfo.sellerAvatarAddress;
                if (!states.TryGetAvatarState(productInfo.sellerAgentAddress,
                        avatarAddress,
                        out var sellerAvatarState))
                {
                    purchaseResult.errorCode = ERROR_CODE_FAILED_LOADING_STATE;
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}BuyMultiple Get Seller AgentAvatarState: {Elapsed}", avatarAddress, sw.Elapsed);
                sw.Restart();

                if (!shopItem.SellerAgentAddress.Equals(productInfo.sellerAgentAddress))
                {
                    purchaseResult.errorCode = ERROR_CODE_ITEM_DOES_NOT_EXIST;
                    continue;
                }
                sw.Stop();
                Log.Verbose("{AddressesHex}BuyMultiple Get Item: {Elapsed}", addressesHex, sw.Elapsed);

                if (0 < shopItem.ExpiredBlockIndex && shopItem.ExpiredBlockIndex < context.BlockIndex)
                {
                    purchaseResult.errorCode = ERROR_CODE_SHOPITEM_EXPIRED;
                    continue;
                }

                // Check buyer's balance
                FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
                if (buyerBalance < shopItem.Price)
                {
                    purchaseResult.errorCode = ERROR_CODE_INSUFFICIENT_BALANCE;
                    continue;
                }

                var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
                var taxedPrice = shopItem.Price - tax;

                // Transfer tax
                states = states.TransferAsset(
                    context.Signer,
                    GoldCurrencyState.Address,
                    tax);

                // Transfer paid money (taxed) to the seller.
                states = states.TransferAsset(
                    context.Signer,
                    productInfo.sellerAgentAddress,
                    taxedPrice
                );

                productDict = (Dictionary)productDict.Remove(productIdSerialized);
                shopStateDict = shopStateDict.SetItem("products", productDict);

                var buyerMail = new BuyerMail(purchaseResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
                purchaseResult.id = buyerMail.id;

                var sellerResultToAdd = new Buy7.SellerResult
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

                buyerAvatarState.Update(buyerMail);
                if (purchaseResult.itemUsable != null)
                {
                    buyerAvatarState.UpdateFromAddItem2(purchaseResult.itemUsable, false);
                }
                if (purchaseResult.costume != null)
                {
                    buyerAvatarState.UpdateFromAddCostume(purchaseResult.costume, false);
                }
                sellerAvatarState.Update(sellerMail);

                // Update quest.
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);

                sellerAvatarState.updatedAt = ctx.BlockIndex;
                sellerAvatarState.blockIndex = ctx.BlockIndex;
                sellerAvatarState.UpdateQuestRewards2(materialSheet);

                sw.Restart();
                states = states.SetState(productInfo.sellerAvatarAddress, sellerAvatarState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}BuyMultiple Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            buyerResult.purchaseResults = purchaseResults;
            sellerResult.sellerResults = sellerResults;

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;

            buyerAvatarState.UpdateQuestRewards2(materialSheet);

            sw.Restart();
            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}BuyMultiple Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            states = states.SetState(ShopState.Address, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}BuyMultiple Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug("{AddressesHex}BuyMultiple Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
