using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sell")]
    public class Sell : GameAction
    {
        public Address sellerAvatarAddress;
        public Guid productId;
        public Guid itemId;
        public decimal price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
            ["itemId"] = itemId.Serialize(),
            ["price"] = price.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
            itemId = plainValue["itemId"].ToGuid();
            price = plainValue["price"].ToDecimal();
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


            if (price < 0)
            {
                return LogError(context, "Aborted as the price is less than zero: {Price}.", price);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }
            sw.Stop();
            Log.Debug("Sell Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();
            
            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
            {
                return LogError(context, "Aborted as the WorldInformation was failed to load.");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInShop)
            {
                // 스테이지 클리어 부족 에러.
                return LogError(
                    context,
                    "Aborted as the signer is not cleared the minimum stage level required to sell items yet: {ClearedLevel} < {RequiredLevel}.",
                    world.StageClearedId,
                    GameConfig.RequireClearedStageLevel.ActionsInShop
                );
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return LogError(context, "Aborted as the shop state was failed to load.");
            }
            var shopState = new ShopState(d);
            sw.Stop();
            Log.Debug("Sell Get ShopState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            Log.Debug("Execute Sell; seller: {SellerAvatarAddress}", sellerAvatarAddress);

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable nonFungibleItem))
            {
                return LogError(
                    context,
                    "Aborted as the NonFungibleItem ({@Item}) was failed to load from avatar's inventory.",
                    itemId
                );
            }

            if (nonFungibleItem.RequiredBlockIndex > context.BlockIndex)
            {
                // 필요 블럭 인덱스 불충분 에러.
                return LogError(
                    context,
                    "Aborted as the equipment to enhance ({@Item}) is not available yet; it will be available at the block #{RequiredBlockIndex}.",
                    itemId,
                    nonFungibleItem.RequiredBlockIndex
                );
            }

            avatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
            if (nonFungibleItem is Equipment equipment)
            {
                equipment.equipped = false;
            }

            // 상점에 아이템을 등록한다.
            shopState.Register(ctx.Signer, new ShopItem(
                sellerAvatarAddress,
                productId,
                nonFungibleItem,
                price
            ));
            sw.Stop();
            Log.Debug("Sell Get Register Item: {Elapsed}", sw.Elapsed);
            sw.Restart();

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("Sell Set AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("Sell Set ShopState: {Elapsed}", sw.Elapsed);
            Log.Debug("Sell Total Executed Time: {Elapsed}", ended - started);

            return states;
        }
    }
}
