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
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("sell5")]
    public class Sell5 : GameAction, ISellV2
    {
        public const long ExpiredBlockIndex = 16000;

        public Address sellerAvatarAddress;
        public Guid tradableId;
        public int count;
        public FungibleAssetValue price;
        public ItemSubType itemSubType;

        Address ISellV2.SellerAvatarAddress => sellerAvatarAddress;
        Guid ISellV2.TradableId => tradableId;
        int ISellV2.Count => count;
        FungibleAssetValue ISellV2.Price => price;
        string ISellV2.ItemSubType => itemSubType.ToString();

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [ItemIdKey] = tradableId.Serialize(),
                [ItemCountKey] = count.Serialize(),
                [PriceKey] = price.Serialize(),
                [ItemSubTypeKey] = itemSubType.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            tradableId = plainValue[ItemIdKey].ToGuid();
            count = plainValue[ItemCountKey].ToInteger();
            price = plainValue[PriceKey].ToFungibleAssetValue();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                states = ShardedShopState.AddressKeys.Aggregate(
                    states,
                    (current, addressKey) => current.SetState(
                        ShardedShopState.DeriveAddress(itemSubType, addressKey),
                        MarkChanged));
                return states.SetState(context.Signer, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell exec started", addressesHex);

            if (price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex}Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStates(
                context.Signer,
                sellerAvatarAddress,
                out _,
                out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Sell Get AgentAvatarStates: {Elapsed}",
                addressesHex,
                sw.Elapsed);
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
            Log.Verbose("{AddressesHex}Sell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            switch (itemSubType)
            {
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                    throw new InvalidShopItemException(
                        $"{addressesHex}Aborted because {nameof(itemSubType)}({itemSubType}) does not support.");
            }

            if (count < 1)
            {
                throw new InvalidShopItemException(
                    $"{addressesHex}Aborted because {nameof(count)}({count}) should be greater than or equal to 1.");
            }

            if (!avatarState.inventory.TryGetTradableItems(tradableId, context.BlockIndex, count, out List<Inventory.Item> inventoryItems))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted because the tradable item({tradableId}) was failed to load from avatar's inventory.");
            }

            IEnumerable<ITradableItem> tradableItems = inventoryItems.Select(i => (ITradableItem)i.item).ToList();
            var expiredBlockIndex = context.BlockIndex + ExpiredBlockIndex;

            foreach (var ti in tradableItems)
            {
                if (!ti.ItemSubType.Equals(itemSubType))
                {
                    throw new InvalidItemTypeException(
                        $"{addressesHex}Expected ItemSubType: {ti.ItemSubType}. Actual ItemSubType: {itemSubType}");
                }

                if (ti is INonFungibleItem)
                {
                    if (count != 1)
                    {
                        throw new ArgumentOutOfRangeException(
                            $"{addressesHex}Aborted because {nameof(count)}({count}) should be 1 because {nameof(tradableId)}({tradableId}) is non-fungible item.");
                    }
                }
            }

            ITradableItem tradableItem = avatarState.inventory.SellItem(tradableId, context.BlockIndex, count);

            var productId = context.Random.GenerateRandomGuid();
            var shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
            if (!states.TryGetState(shardedShopAddress, out BxDictionary serializedSharedShopState))
            {
                var shardedShopState = new ShardedShopState(shardedShopAddress);
                serializedSharedShopState = (BxDictionary) shardedShopState.Serialize();
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Sell Get ShardedShopState: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            var serializedProductList = (BxList) serializedSharedShopState[ProductsKey];
            string productKey;
            string itemIdKey;
            string requiredBlockIndexKey;
            switch (tradableItem.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    productKey = LegacyItemUsableKey;
                    itemIdKey = LegacyItemIdKey;
                    requiredBlockIndexKey = LegacyRequiredBlockIndexKey;
                    break;
                case ItemType.Costume:
                    productKey = LegacyCostumeKey;
                    itemIdKey = LegacyCostumeItemIdKey;
                    requiredBlockIndexKey = RequiredBlockIndexKey;
                    break;
                case ItemType.Material:
                    productKey = TradableFungibleItemKey;
                    itemIdKey = LegacyCostumeItemIdKey;
                    requiredBlockIndexKey = RequiredBlockIndexKey;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            BxDictionary serializedProductDictionary;
            if (tradableItem.ItemType == ItemType.Material)
            {
                // Find expired TradableMaterial
                serializedProductDictionary = serializedProductList
                    .Select(p => (BxDictionary) p)
                    .FirstOrDefault(p =>
                    {
                        var materialItemId =
                            ((BxDictionary) p[productKey])[itemIdKey].ToItemId();
                        var requiredBlockIndex = p[ExpiredBlockIndexKey].ToLong();
                        return TradableMaterial.DeriveTradableId(materialItemId)
                            .Equals(tradableItem.TradableId) && requiredBlockIndex <= context.BlockIndex;
                    });
            }
            else
            {
                var serializedTradeId = tradableItem.TradableId.Serialize();
                serializedProductDictionary = serializedProductList
                    .Select(p => (BxDictionary) p)
                    .FirstOrDefault(p =>
                        ((BxDictionary) p[productKey])[itemIdKey].Equals(serializedTradeId));
            }

            // Since Bencodex 0.4, Dictionary/List are reference types; so their default values
            // are not a empty container, but a null reference:
            serializedProductDictionary = serializedProductDictionary ?? Dictionary.Empty;

            ShopItem shopItem;
            // Register new ShopItem
            if (serializedProductDictionary.Equals(BxDictionary.Empty))
            {
                shopItem = new ShopItem(
                    context.Signer,
                    sellerAvatarAddress,
                    productId,
                    price,
                    expiredBlockIndex,
                    tradableItem,
                    count);
                var serializedShopItem = shopItem.Serialize();
                serializedProductList = serializedProductList.Add(serializedShopItem);
            }
            // Update Registered ShopItem
            else
            {
                // Delete current ShopItem
                serializedProductList =
                    (BxList) serializedProductList.Remove(serializedProductDictionary);

                // Update ITradableItem.RequiredBlockIndex
                var inChainShopItem = (BxDictionary) serializedProductDictionary[productKey];
                inChainShopItem = inChainShopItem
                    .SetItem(requiredBlockIndexKey, expiredBlockIndex.Serialize());

                // Update ShopItem.ExpiredBlockIndex
                serializedProductDictionary = serializedProductDictionary
                    .SetItem(ExpiredBlockIndexKey, expiredBlockIndex.Serialize())
                    .SetItem(productKey, inChainShopItem);

                // Update only Material for backwardCompatible.
                if (tradableItem.ItemType == ItemType.Material)
                {
                    serializedProductDictionary = serializedProductDictionary
                        .SetItem(TradableFungibleItemCountKey, count.Serialize());
                }

                serializedProductList = serializedProductList.Add(serializedProductDictionary);
                shopItem = new ShopItem(serializedProductDictionary);
            }

            serializedSharedShopState = serializedSharedShopState.SetItem(
                ProductsKey, serializedProductList);

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Get Register Item: {Elapsed}", addressesHex,
                sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            var result = new SellCancellation.Result
            {
                shopItem = shopItem,
                itemUsable = shopItem.ItemUsable,
                costume = shopItem.Costume,
                tradableFungibleItem = shopItem.TradableFungibleItem,
                tradableFungibleItemCount = shopItem.TradableFungibleItemCount,
            };
            var mail = new SellCancelMail(
                result,
                context.BlockIndex,
                context.Random.GenerateRandomGuid(),
                expiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(shardedShopAddress, serializedSharedShopState);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose(
                "{AddressesHex}Sell Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);

            return states;
        }
    }
}
