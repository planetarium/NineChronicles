using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Item;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("sell_cancellation")]
    public class SellCancellation : GameAction
    {
        [Serializable]
        public class ResultModel
        {
            public Address owner;
            public ShopItem shopItem;
        }

        public Address sellerAvatarAddress;
        public Guid productId;

        public ResultModel result;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.ToByteArray(),
            ["productId"] = productId.ToByteArray(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            sellerAvatarAddress = new Address((byte[]) plainValue["sellerAvatarAddress"]);
            productId = new Guid((byte[]) plainValue["productId"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var avatarState = (AvatarState) states.GetState(ctx.Signer);
            if (avatarState == null)
            {
                return SimpleError(ctx, ErrorCode.AvatarNotFound);
            }
            
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();

            ShopItem target;
            try
            {
                // 상점에서 아이템을 빼온다.
                target = shopState.Unregister(sellerAvatarAddress, productId);
            }
            catch
            {
                return SimpleError(ctx, ErrorCode.UnexpectedCaseInActionExecute);
            }
            
            // 인벤토리에 아이템을 넣는다.
            avatarState.inventory.AddNonFungibleItem(target.itemUsable);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            
            result = new ResultModel
            {
                owner = sellerAvatarAddress,
                shopItem = target
            };

            states = states.SetState(ctx.Signer, avatarState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
