using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Mail;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("sell_cancellation")]
    public class SellCancellation : GameAction
    {
        public Guid productId;
        public Address sellerAvatarAddress;
        public Result result;

        [Serializable]
        public class Result : AttachmentActionResult
        {
            public Game.Item.ShopItem shopItem;
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["productId"] = productId.ToByteArray(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.ToByteArray(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            productId = new Guid((byte[]) plainValue["productId"]);
            sellerAvatarAddress = new Address((byte[]) plainValue["sellerAvatarAddress"]);
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

            var agentState = (AgentState) states.GetState(ctx.Signer);
            if (!agentState.avatarAddresses.ContainsValue(sellerAvatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(sellerAvatarAddress);
            if (avatarState == null)
            {
                return states;
            }

            var shopState = (ShopState) states.GetState(ShopState.Address);

            // 상점에서 아이템을 빼온다.
            if (!shopState.TryUnregister(ctx.Signer, productId, out var outUnregisteredItem))
            {
                return states;
            }
            
            // 메일에 아이템을 넣는다.
            result = new Result
            {
                shopItem = outUnregisteredItem,
                itemUsable = outUnregisteredItem.itemUsable
            };
            var mail = new SellCancelMail(result, ctx.BlockIndex);
            avatarState.Update(mail);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.BlockIndex = ctx.BlockIndex;
            
            states = states.SetState(ctx.Signer, agentState);
            states = states.SetState(sellerAvatarAddress, avatarState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
