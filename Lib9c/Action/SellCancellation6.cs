using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("sell_cancellation6")]
    public class SellCancellation6 : GameAction
    {
        public Guid productId;
        public Address sellerAvatarAddress;
        public SellCancellation.Result result;
        public ItemSubType itemSubType;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [ProductIdKey] = productId.Serialize(),
            [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
            [ItemSubTypeKey] = itemSubType.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            productId = plainValue[ProductIdKey].ToGuid();
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
            if (context.Rehearsal)
            {
                states = states.SetState(shardedShopAddress, MarkChanged);
                return states
                    .SetState(Addresses.Shop, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged);
            }

            CheckObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Cancel exec started", addressesHex);

            if (!states.TryGetAvatarState(context.Signer, sellerAvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the seller failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            if (!states.TryGetState(shardedShopAddress, out BxDictionary shopStateDict))
            {
                var shopState = new ShardedShopState(shardedShopAddress);
                shopStateDict = (BxDictionary) shopState.Serialize();
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            // 상점에서 아이템을 빼온다.
            var products = (BxList)shopStateDict[ProductsKey];
            var productIdSerialized = productId.Serialize();
            var productSerialized = products
                .Select(p => (BxDictionary) p)
                .FirstOrDefault(p => p[LegacyProductIdKey].Equals(productIdSerialized));

            // Since Bencodex 0.4, Dictionary/List are reference types; so their default values
            // are not a empty container, but a null reference:
            productSerialized = productSerialized ?? Dictionary.Empty;

            var backwardCompatible = false;
            if (productSerialized.Equals(BxDictionary.Empty))
            {
                if (itemSubType == ItemSubType.Hourglass || itemSubType == ItemSubType.ApStone)
                {
                    throw new ItemDoesNotExistException(
                        $"{addressesHex}Aborted as the shop item ({productId}) could not be found from the shop.");
                }
                // Backward compatibility.
                var rawShop = states.GetState(Addresses.Shop);
                if (!(rawShop is null))
                {
                    var legacyShopDict = (BxDictionary) rawShop;
                    var legacyProducts = (BxDictionary) legacyShopDict[LegacyProductsKey];
                    var productKey = (IKey) productId.Serialize();
                    // SoldOut
                    if (!legacyProducts.ContainsKey(productKey))
                    {
                        throw new ItemDoesNotExistException(
                            $"{addressesHex}Aborted as the shop item ({productId}) could not be found from the legacy shop."
                        );
                    }

                    productSerialized = (BxDictionary) legacyProducts[productKey];
                    legacyProducts = (BxDictionary) legacyProducts.Remove(productKey);
                    legacyShopDict = legacyShopDict.SetItem(LegacyProductsKey, legacyProducts);
                    states = states.SetState(Addresses.Shop, legacyShopDict);
                    backwardCompatible = true;
                }
            }
            else
            {
                products = (BxList) products.Remove(productSerialized);
                shopStateDict = shopStateDict.SetItem(ProductsKey, products);
            }

            var shopItem = new ShopItem(productSerialized);

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Get Unregister Item: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (shopItem.SellerAvatarAddress != sellerAvatarAddress || shopItem.SellerAgentAddress != context.Signer)
            {
                throw new InvalidAddressException($"{addressesHex}Invalid Avatar Address");
            }

            ITradableItem tradableItem;
            int itemCount = 1;
            if (!(shopItem.ItemUsable is null))
            {
                tradableItem = shopItem.ItemUsable;
            }
            else if (!(shopItem.Costume is null))
            {
                tradableItem = shopItem.Costume;
            }
            else if (!(shopItem.TradableFungibleItem is null))
            {
                tradableItem = shopItem.TradableFungibleItem;
                itemCount = shopItem.TradableFungibleItemCount;
            }
            else
            {
                throw new InvalidShopItemException($"{addressesHex}Tradable Item is null.");
            }

            if (!backwardCompatible)
            {
                avatarState.inventory.UpdateTradableItem(tradableItem.TradableId,
                    tradableItem.RequiredBlockIndex, itemCount, context.BlockIndex);
            }

            if (tradableItem is INonFungibleItem nonFungibleItem)
            {
                nonFungibleItem.RequiredBlockIndex = context.BlockIndex;
                if (backwardCompatible)
                {
                    switch (nonFungibleItem)
                    {
                        case ItemUsable itemUsable:
                            avatarState.UpdateFromAddItem2(itemUsable, true);
                            break;
                        case Costume costume:
                            avatarState.UpdateFromAddCostume(costume, true);
                            break;
                    }
                }
            }

            // 메일에 아이템을 넣는다.
            result = new SellCancellation.Result
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                costume = shopItem.Costume,
                tradableFungibleItem = shopItem.TradableFungibleItem,
                tradableFungibleItemCount = shopItem.TradableFungibleItemCount,
            };
            var mail = new SellCancelMail(result, context.BlockIndex, context.Random.GenerateRandomGuid(), context.BlockIndex);
            result.id = mail.id;

            avatarState.Update(mail);
            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Cancel Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(shardedShopAddress, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Cancel Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex}Sell Cancel Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states;
        }
    }
}
