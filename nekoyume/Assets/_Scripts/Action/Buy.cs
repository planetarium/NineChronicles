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
        [Serializable]
        public class ResultModel
        {
            public Address owner;
            public ShopItem shopItem;
        }

        public Address buyerAgentAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;

        public ResultModel result;

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
                return SimpleError(ctx, ErrorCode.BuyBuyerAgentNotFound);
            }
            
            var buyerAvatarState = (AvatarState) states.GetState(ctx.Signer);
            if (buyerAvatarState == null)
            {
                return SimpleError(ctx, ErrorCode.BuyBuyerAvatarNotFound);
            }
            
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();
            
            try
            {
                // 상점에서 구매할 아이템을 찾는다.
                var target = shopState.Find(sellerAvatarAddress, productId);
                var sellerAgentState = (AgentState) states.GetState(target.Value.sellerAgentAddress);
                if (sellerAgentState == null)
                {
                    return SimpleError(ctx, ErrorCode.BuySellerAgentNotFound);
                }
                
                // 돈은 있냐?
                if (buyerAgentState.gold < target.Value.price)
                {
                    return SimpleError(ctx, ErrorCode.BuyGoldNotEnough);
                }

                // 상점에서 구매할 아이템을 제거한다.
                if (!shopState.Unregister(target.Key, target.Value))
                {
                    return SimpleError(ctx, ErrorCode.UnexpectedCaseInActionExecute);
                }
                
                // 구매자의 돈을 감소 시킨다.
                buyerAgentState.gold -= target.Value.price;
                
                // 판매자의 돈을 증가 시킨다.
                sellerAgentState.gold += target.Value.price;

                // 구매자의 인벤토리에 구매한 아이템을 넣는다.
                buyerAvatarState.inventory.AddNonFungibleItem(target.Value.itemUsable);
                buyerAvatarState.updatedAt = DateTimeOffset.UtcNow;

                result = new ResultModel
                {
                    owner = target.Key,
                    shopItem = target.Value,
                };

                states = states.SetState(buyerAgentAddress, buyerAgentState);
                states = states.SetState(ctx.Signer, buyerAvatarState);
                states = states.SetState(target.Value.sellerAgentAddress, sellerAgentState);
                return states.SetState(ShopState.Address, shopState);
            }
            catch (KeyNotFoundException)
            {
                return SimpleError(ctx, ErrorCode.BuySoldOut);
            }
            catch
            {
                return SimpleError(ctx, ErrorCode.UnexpectedCaseInActionExecute);
            }
        }
    }
}
