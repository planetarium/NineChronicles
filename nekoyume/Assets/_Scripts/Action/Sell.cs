using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        public Address sellerAvatarAddress;
        public Guid productId;
        public ItemUsable itemUsable;
        public decimal price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
            ["itemUsable"] = itemUsable.Serialize(),
            ["price"] = price.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
            itemUsable = (ItemUsable) ItemFactory.Deserialize(
                (Bencodex.Types.Dictionary) plainValue["itemUsable"]
            );
            price = plainValue["price"].ToDecimal();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
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
            UnityEngine.Debug.Log($"Sell exec started.");


            if (price < 0)
            {
                return states;
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }
            sw.Stop();
            UnityEngine.Debug.Log($"Sell Get AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            var shopState = new ShopState(d);
            sw.Stop();
            UnityEngine.Debug.Log($"Sell Get ShopState: {sw.Elapsed}");
            sw.Restart();

            UnityEngine.Debug.Log($"Execute Sell. seller : `{sellerAvatarAddress}` " +
                      $"node : `{States.Instance?.AgentState?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.address}`");

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemUsable, out ItemUsable nonFungibleItem))
            {
                return states;
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
            UnityEngine.Debug.Log($"Sell Get Register Item: {sw.Elapsed}");
            sw.Restart();

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            UnityEngine.Debug.Log($"Sell Set AvatarState: {sw.Elapsed}");
            sw.Restart();

            states = states.SetState(ShopState.Address, shopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            UnityEngine.Debug.Log($"Sell Set ShopState: {sw.Elapsed}");
            UnityEngine.Debug.Log($"Sell Total Executed Time: {ended - started}");

            return states;
        }
    }
}
