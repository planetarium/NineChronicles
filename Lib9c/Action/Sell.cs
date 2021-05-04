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
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sell5")]
    public class Sell : GameAction
    {
        public const long ExpiredBlockIndex = 16000;

        public Address sellerAvatarAddress;
        public Guid tradableId;
        public int count;
        public FungibleAssetValue price;
        public ItemSubType itemSubType;

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

            if (!avatarState.inventory.TryGetTradableItem(
                    tradableId,
                    out var inventoryItem) ||
                !(inventoryItem.item is ITradableItem tradableItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted because the tradable item({tradableId}) was failed to load from avatar's inventory.");
            }

            if (inventoryItem.count < count)
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted because inventory item count({inventoryItem.count}) should be greater than or equal to {nameof(count)}({count}).");
            }

            if (!tradableItem.ItemSubType.Equals(itemSubType))
            {
                throw new InvalidItemTypeException(
                    $"{addressesHex}Expected ItemSubType: {tradableItem.ItemSubType}. Actual ItemSubType: {itemSubType}");
            }

            switch (tradableItem)
            {
                case INonFungibleItem _ when count != 1:
                    throw new ArgumentOutOfRangeException(
                        $"{addressesHex}Aborted because {nameof(count)}({count}) should be 1 because {nameof(tradableId)}({tradableId}) is non-fungible item.");
                case INonFungibleItem nonFungibleItem when nonFungibleItem.RequiredBlockIndex > context.BlockIndex:
                    throw new RequiredBlockIndexException(
                        $"{addressesHex}Aborted because the non-fungible item({tradableId}) to sell is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}.");
            }

            var expiredBlockIndex = context.BlockIndex + ExpiredBlockIndex;
            tradableItem.RequiredBlockIndex = expiredBlockIndex;

            if (tradableItem is IEquippableItem equippableItem)
            {
                equippableItem.Unequip();
            }

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
            switch (tradableItem.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Costume:
                case ItemType.Equipment:
                    var serializedTradeId = tradableItem.TradableId.Serialize();
                    serializedProductDictionary = serializedProductList
                        .Select(p => (BxDictionary) p)
                        .FirstOrDefault(p =>
                            ((BxDictionary) p[productKey])[itemIdKey].Equals(serializedTradeId));
                    break;
                case ItemType.Material:
                    serializedProductDictionary = serializedProductList
                        .Select(p => (BxDictionary) p)
                        .FirstOrDefault(p =>
                        {
                            var materialItemId =
                                ((BxDictionary) p[productKey])[itemIdKey].ToItemId();
                            return TradableMaterial.DeriveTradableId(materialItemId)
                                .Equals(tradableItem.TradableId);
                        });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
                    .SetItem(requiredBlockIndexKey, expiredBlockIndex.Serialize())
                    .SetItem(TradableFungibleItemCountKey, count.Serialize());

                // Update ShopItem.ExpiredBlockIndex
                serializedProductDictionary = serializedProductDictionary
                    .SetItem(ExpiredBlockIndexKey, expiredBlockIndex.Serialize())
                    .SetItem(productKey, inChainShopItem);
                serializedProductList = serializedProductList.Add(serializedProductDictionary);
                shopItem = new ShopItem(serializedProductDictionary);
            }

            serializedSharedShopState = serializedSharedShopState.SetItem(
                ProductsKey,
                new List<IValue>(serializedProductList));

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
                costume = shopItem.Costume
            };
            var mail = new SellCancelMail(
                result,
                context.BlockIndex,
                context.Random.GenerateRandomGuid(),
                expiredBlockIndex);
            result.id = mail.id;
            avatarState.UpdateV3(mail);

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
