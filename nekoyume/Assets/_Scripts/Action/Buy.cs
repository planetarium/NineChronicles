using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("buy")]
    public class Buy : GameAction
    {
        public Address buyerAvatarAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;
        public BuyerResult buyerResult;
        public SellerResult sellerResult;

        [Serializable]
        public class BuyerResult : AttachmentActionResult
        {
            public Game.Item.ShopItem shopItem;
        }

        [Serializable]
        public class SellerResult : AttachmentActionResult
        {
            public Game.Item.ShopItem shopItem;
            public decimal gold;
        }

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

            var shopState = (ShopState) states.GetState(ShopState.Address);

            Debug.Log($"Execute Buy. buyer : `{buyerAvatarAddress}` seller: `{sellerAvatarAddress}`" +
                      $"node : `{States.Instance?.AgentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.Value?.address}`");
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

            var sellerAvatarState = (AvatarState) states.GetState(outPair.Value.SellerAvatarAddress);
            if (sellerAvatarState is null)
                return states;

            // 돈은 있냐?
            if (buyerAgentState.gold < outPair.Value.Price)
            {
                return states;
            }
            
            // 상점에서 구매할 아이템을 제거한다.
            if (!shopState.Unregister(sellerAgentAddress, outPair.Value))
            {
                return states;
            }
            
            // 구매자의 돈을 감소 시킨다.
            buyerAgentState.gold -= outPair.Value.Price;
                
            // 구매자, 판매자에게 결과 메일 전송
            buyerResult = new BuyerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable
            };
            var buyerMail = new BuyerMail(buyerResult, ctx.BlockIndex);
            buyerAvatarState.Update(buyerMail);

            sellerResult = new SellerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable,
                gold = decimal.Round(outPair.Value.Price * 0.92m)
            };
            var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex);
            sellerAvatarState.Update(sellerMail);

            // 퀘스트 업데이트
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell);

            var timestamp = DateTimeOffset.UtcNow;
            buyerAvatarState.updatedAt = timestamp;
            buyerAvatarState.BlockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = timestamp;
            sellerAvatarState.BlockIndex = ctx.BlockIndex;

            states = states.SetState(buyerAvatarAddress, buyerAvatarState);
            states = states.SetState(ctx.Signer, buyerAgentState);
            states = states.SetState(sellerAvatarAddress, sellerAvatarState);
            states = states.SetState(sellerAgentAddress, sellerAgentState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
