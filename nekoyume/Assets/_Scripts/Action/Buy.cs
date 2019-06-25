using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Item;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("buy")]
    public class Buy : GameAction
    {
        public Address buyerAgentAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["buyerAgentAddress"] = buyerAgentAddress.ToByteArray(),
            ["sellerAgentAddress"] = sellerAgentAddress.ToByteArray(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.ToByteArray(),
            ["productId"] = productId.ToByteArray(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            buyerAgentAddress = new Address((byte[]) plainValue["buyerAgentAddress"]);
            sellerAgentAddress = new Address((byte[]) plainValue["sellerAgentAddress"]);
            sellerAvatarAddress = new Address((byte[]) plainValue["sellerAvatarAddress"]);
            productId = new Guid((byte[]) plainValue["productId"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {   
                states = states.SetState(buyerAgentAddress, MarkChanged);
                states = states.SetState(ctx.Signer, MarkChanged);
                states = states.SetState(sellerAgentAddress, MarkChanged);
                return states.SetState(ShopState.Address, MarkChanged);
            }

            var buyerAgentState = (AgentState) states.GetState(buyerAgentAddress);
            if (buyerAgentState == null)
            {
                return states;
            }
            
            var buyerAvatarState = (AvatarState) states.GetState(ctx.Signer);
            if (buyerAvatarState == null)
            {
                return states;
            }
            
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();

            // 상점에서 구매할 아이템을 찾는다.
            if (!shopState.TryGet(sellerAvatarAddress, productId, out var outPair))
            {
                return states;
            }
            
            var sellerAgentState = (AgentState) states.GetState(outPair.Value.sellerAgentAddress);
            if (sellerAgentState == null)
            {
                return states;
            }
            
            // 돈은 있냐?
            if (buyerAgentState.gold < outPair.Value.price)
            {
                return states;
            }
            
            // 상점에서 구매할 아이템을 제거한다.
            if (!shopState.Unregister(outPair.Key, outPair.Value))
            {
                return states;
            }
            
            // 구매자의 돈을 감소 시킨다.
            buyerAgentState.gold -= outPair.Value.price;
                
            // 판매자의 돈을 증가 시킨다.
            sellerAgentState.gold += outPair.Value.price;
            
            // 구매자의 인벤토리에 구매한 아이템을 넣는다.
            buyerAvatarState.inventory.AddUnfungibleItem(outPair.Value.itemUsable);
            buyerAvatarState.updatedAt = DateTimeOffset.UtcNow;

            states = states.SetState(buyerAgentAddress, buyerAgentState);
            states = states.SetState(ctx.Signer, buyerAvatarState);
            states = states.SetState(outPair.Value.sellerAgentAddress, sellerAgentState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
