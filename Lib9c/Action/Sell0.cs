using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("sell")]
    public class Sell0 : GameAction, ISellV1
    {
        public Address sellerAvatarAddress;
        public Guid itemId;
        public FungibleAssetValue price;

        Address ISellV1.SellerAvatarAddress => sellerAvatarAddress;
        Guid ISellV1.ItemId => itemId;
        FungibleAssetValue ISellV1.Price => price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["itemId"] = itemId.Serialize(),
            ["price"] = price.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            itemId = plainValue["itemId"].ToGuid();
            price = plainValue["price"].ToFungibleAssetValue();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell exec started", addressesHex);


            if (price.Sign < 0)
            {
                throw new InvalidPriceException($"{addressesHex}Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("{AddressesHex}Sell Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            Log.Debug("{AddressesHex}Sell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);

            sw.Restart();
            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary shopStateDict))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the shop state was failed to load.");
            }

            Log.Debug("{AddressesHex}Sell Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Debug("{AddressesHex}Execute Sell; seller: {SellerAvatarAddress}", addressesHex, sellerAvatarAddress);

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (nonFungibleItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the equipment to sell ({itemId}) is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}."
                );
            }

            avatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
            if (nonFungibleItem is Equipment equipment)
            {
                equipment.equipped = false;
            }

            var productId = context.Random.GenerateRandomGuid();

            var shopItem = new ShopItem(
                ctx.Signer,
                sellerAvatarAddress,
                productId,
                price,
                nonFungibleItem);

            IValue shopItemSerialized = shopItem.Serialize();
            IKey productIdSerialized = (IKey)productId.Serialize();

            Dictionary products = (Dictionary)shopStateDict["products"];
            products = (Dictionary)products.Add(productIdSerialized, shopItemSerialized);
            shopStateDict = shopStateDict.SetItem("products", products);

            sw.Stop();
            Log.Debug("{AddressesHex}Sell Get Register Item: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("{AddressesHex}Sell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug("{AddressesHex}Sell Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
