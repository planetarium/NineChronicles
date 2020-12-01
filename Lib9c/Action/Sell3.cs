using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sell3")]
    public class Sell3 : GameAction
    {
        public Address sellerAvatarAddress;
        public Guid itemId;
        public FungibleAssetValue price;

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
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("Sell exec started.");


            if (price.Sign < 0)
            {
                throw new InvalidPriceException($"Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("Sell Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            Log.Debug("Sell IsStageCleared: {Elapsed}", sw.Elapsed);

            sw.Restart();

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary shopStateDict))
            {
                throw new FailedLoadStateException("Aborted as the shop state was failed to load.");
            }

            Log.Debug("Sell Get ShopState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            Log.Debug("Execute Sell; seller: {SellerAvatarAddress}", sellerAvatarAddress);

            var productId = context.Random.GenerateRandomGuid();
            ShopItem shopItem;

            void CheckRequiredBlockIndex(ItemUsable itemUsable)
            {
                if (itemUsable.RequiredBlockIndex > context.BlockIndex)
                {
                    throw new RequiredBlockIndexException($"Aborted as the itemUsable to enhance ({itemId}) is not available yet; it will be available at the block #{itemUsable.RequiredBlockIndex}.");
                }
            }

            ShopItem PopShopItemFromInventory(ItemUsable itemUsable, Costume costume)
            {
                avatarState.inventory.RemoveNonFungibleItem(itemId);
                return itemUsable is null
                    ? new ShopItem(ctx.Signer, sellerAvatarAddress, productId, price, costume)
                    : new ShopItem(ctx.Signer, sellerAvatarAddress, productId, price, itemUsable);
            }

            // Select an item to sell from the inventory and adjust the quantity.
            if (avatarState.inventory.TryGetNonFungibleItem<Equipment>(itemId, out var equipment))
            {
                CheckRequiredBlockIndex(equipment);
                equipment.equipped = false;
                shopItem = PopShopItemFromInventory(equipment, null);
            }
            else if (avatarState.inventory.TryGetNonFungibleItem<Consumable>(itemId, out var consumable))
            {
                CheckRequiredBlockIndex(consumable);
                avatarState.inventory.RemoveNonFungibleItem(itemId);
                shopItem = PopShopItemFromInventory(consumable, null);
            }
            else if (avatarState.inventory.TryGetNonFungibleItem<Costume>(itemId, out var costume))
            {
                costume.equipped = false;
                shopItem = PopShopItemFromInventory(null, costume);
            }
            else
            {
                throw new ItemDoesNotExistException(
                    $"Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory.");
            }

            IValue shopItemSerialized = shopItem.Serialize();
            IKey productIdSerialized = (IKey)productId.Serialize();

            Dictionary products = (Dictionary)shopStateDict["products"];
            products = (Dictionary)products.Add(productIdSerialized, shopItemSerialized);
            shopStateDict = shopStateDict.SetItem("products", products);

            sw.Stop();
            Log.Debug("Sell Get Register Item: {Elapsed}", sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = ctx.BlockIndex;
            avatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("Sell Set AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopStateDict);
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("Sell Set ShopState: {Elapsed}", sw.Elapsed);
            Log.Debug("Sell Total Executed Time: {Elapsed}", ended - started);

            return states;
        }
    }
}
