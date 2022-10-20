using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("sell4")]
    public class Sell4 : GameAction
    {
        public const long ExpiredBlockIndex = 16000;
        public Address sellerAvatarAddress;
        public Guid itemId;
        public ItemSubType itemSubType;
        public FungibleAssetValue price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
            [ItemIdKey] = itemId.Serialize(),
            [PriceKey] = price.Serialize(),
            [ItemSubTypeKey] = itemSubType.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            itemId = plainValue[ItemIdKey].ToGuid();
            price = plainValue[PriceKey].ToFungibleAssetValue();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                states = ShardedShopState.AddressKeys.Aggregate(states,
                    (current, addressKey) =>
                        current.SetState(ShardedShopState.DeriveAddress(itemSubType, addressKey), MarkChanged));
                return states
                    .SetState(ctx.Signer, MarkChanged);
            }

            CheckObsolete(BlockChain.Policy.BlockPolicySource.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell exec started", addressesHex);


            if (price.Sign < 0)
            {
                throw new InvalidPriceException($"{addressesHex}Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            Log.Verbose("{AddressesHex}Sell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();

            Log.Verbose("{AddressesHex}Execute Sell; seller: {SellerAvatarAddress}", addressesHex, sellerAvatarAddress);

            var productId = context.Random.GenerateRandomGuid();
            long expiredBlockIndex = context.BlockIndex + ExpiredBlockIndex;

            // Select an item to sell from the inventory and adjust the quantity.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out INonFungibleItem nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory.");
            }

            ItemSubType nonFungibleItemType = nonFungibleItem is Costume costume
                ? costume.ItemSubType
                : ((ItemUsable) nonFungibleItem).ItemSubType;

            if (!nonFungibleItemType.Equals(itemSubType))
            {
                throw new InvalidItemTypeException($"Expected ItemType: {nonFungibleItemType}. Actual ItemType: {itemSubType}");
            }

            if (nonFungibleItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the itemUsable to sell ({itemId}) is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}.");
            }

            if (nonFungibleItem is Equipment equipment)
            {
                equipment.Unequip();
            }
            nonFungibleItem.RequiredBlockIndex = expiredBlockIndex;

            ShopItem shopItem = new ShopItem(ctx.Signer, sellerAvatarAddress, productId, price, expiredBlockIndex, nonFungibleItem);
            Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
            if (!states.TryGetState(shardedShopAddress, out Bencodex.Types.Dictionary shopStateDict))
            {
                ShardedShopState shardedShopState = new ShardedShopState(shardedShopAddress);
                shopStateDict = (Dictionary) shardedShopState.Serialize();
            }

            Log.Verbose("{AddressesHex}Sell Get ShardedShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            List products = (List)shopStateDict[ProductsKey];
            string productKey = LegacyItemUsableKey;
            string itemIdKey = LegacyItemIdKey;
            string requiredBlockIndexKey = LegacyRequiredBlockIndexKey;
            if (nonFungibleItem is Costume)
            {
                productKey = LegacyCostumeKey;
                itemIdKey = LegacyCostumeItemIdKey;
                requiredBlockIndexKey = RequiredBlockIndexKey;
            }
#pragma warning disable LAA1002
            Dictionary productSerialized = products
                .Select(p => (Dictionary) p)
                .FirstOrDefault(p =>
                    ((Dictionary) p[productKey])[itemIdKey].Equals(nonFungibleItem.NonFungibleId.Serialize()));
#pragma warning restore LAA1002

            // Since Bencodex 0.4, Dictionary/List are reference types; so their default values
            // are not a empty container, but a null reference:
            productSerialized = productSerialized ?? Dictionary.Empty;

            // Register new ShopItem
            if (productSerialized.Equals(Dictionary.Empty))
            {
                IValue shopItemSerialized = shopItem.Serialize();
                products = products.Add(shopItemSerialized);
            }
            // Update Registered ShopItem
            else
            {
                // Delete current ShopItem
                products = (List) products.Remove(productSerialized);

                // Update INonfungibleItem.RequiredBlockIndex
                Dictionary item = (Dictionary) productSerialized[productKey];
                item = item.SetItem(requiredBlockIndexKey, expiredBlockIndex.Serialize());

                // Update ShopItem.ExpiredBlockIndex
                productSerialized = productSerialized
                    .SetItem(ExpiredBlockIndexKey, expiredBlockIndex.Serialize())
                    .SetItem(productKey, item);
                products = products.Add(productSerialized);
                shopItem = new ShopItem(productSerialized);
            }
            shopStateDict = shopStateDict.SetItem(ProductsKey, products);

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Get Register Item: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;

            var result = new SellCancellation.Result
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                costume = shopItem.Costume
            };
            var mail = new SellCancelMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), expiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(shardedShopAddress, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose("{AddressesHex}Sell Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
