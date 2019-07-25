using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("buy")]
    public class Buy : GameAction
    {
        public Address buyerAvatarAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.ToByteArray(),
            ["sellerAgentAddress"] = sellerAgentAddress.ToByteArray(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.ToByteArray(),
            ["productId"] = productId.ToByteArray(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            buyerAvatarAddress = new Address((byte[]) plainValue["buyerAvatarAddress"]);
            sellerAgentAddress = new Address((byte[]) plainValue["sellerAgentAddress"]);
            sellerAvatarAddress = new Address((byte[]) plainValue["sellerAvatarAddress"]);
            productId = new Guid((byte[]) plainValue["productId"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {   
                states = states.SetState(buyerAvatarAddress, MarkChanged);
                states = states.SetState(ctx.Signer, MarkChanged);
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                states = states.SetState(sellerAgentAddress, MarkChanged);
                return states.SetState(ShopState.Address, MarkChanged);
            }

            if (ctx.Signer.Equals(sellerAgentAddress))
                return states;

            var buyerAgentState = (AgentState) states.GetState(ctx.Signer);
            if (buyerAgentState == null)
            {
                return states;
            }

            if (!buyerAgentState.avatarAddresses.ContainsValue(buyerAvatarAddress))
                return states;

            
            var buyerAvatarState = (AvatarState) states.GetState(buyerAvatarAddress);
            if (buyerAvatarState == null)
            {
                return states;
            }
            
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();

            // 상점에서 구매할 아이템을 찾는다.
            if (!shopState.TryGet(sellerAgentAddress, productId, out var outPair))
            {
                return states;
            }

            var sellerAgentState = (AgentState) states.GetState(sellerAgentAddress);
            if (sellerAgentState is null)
            {
                return states;
            }

            if (!sellerAgentState.avatarAddresses.ContainsValue(sellerAvatarAddress))
                return states;

            var sellerAvatarState = (AvatarState) states.GetState(outPair.Value.sellerAvatarAddress);
            if (sellerAvatarState is null)
                return states;

            // 돈은 있냐?
            if (buyerAgentState.gold < outPair.Value.price)
            {
                return states;
            }
            
            // 상점에서 구매할 아이템을 제거한다.
            if (!shopState.Unregister(sellerAgentAddress, outPair.Value))
            {
                return states;
            }
            
            // 구매자의 돈을 감소 시킨다.
            buyerAgentState.gold -= outPair.Value.price;
                
            // 판매자의 돈을 증가 시킨다.
            sellerAgentState.gold += outPair.Value.price;
            
            // 구매자의 인벤토리에 구매한 아이템을 넣는다.
            buyerAvatarState.inventory.AddNonFungibleItem(outPair.Value.itemUsable);

            // 퀘스트 업데이트
            buyerAvatarState.questList.UpdateTradeQuest("buy");
            sellerAvatarState.questList.UpdateTradeQuest("sell");

            var timestamp = DateTimeOffset.UtcNow;
            buyerAvatarState.updatedAt = timestamp;
            sellerAvatarState.updatedAt = timestamp;

            states = states.SetState(buyerAvatarAddress, buyerAvatarState);
            states = states.SetState(ctx.Signer, buyerAgentState);
            states = states.SetState(sellerAvatarAddress, sellerAvatarState);
            states = states.SetState(sellerAgentAddress, sellerAgentState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
