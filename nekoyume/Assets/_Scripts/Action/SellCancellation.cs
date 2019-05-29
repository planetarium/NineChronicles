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

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var avatarState = (AvatarState) states.GetState(actionCtx.Signer);
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();

            ShopItem target;
            try
            {
                // 상점에서 아이템을 빼온다.
                target = shopState.Unregister(sellerAvatarAddress, productId);
            }
            catch
            {
                return SimpleError(actionCtx, avatarState, GameActionErrorCode.UnexpectedInternalAction);
            }
            
            // 인벤토리에 아이템을 넣는다.
            avatarState.AddEquipmentItemToItems(target.item.Data.id, target.count);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
                
            errorCode = GameActionErrorCode.Success;
            result = new ResultModel
            {
                owner = sellerAvatarAddress,
                shopItem = target
            };

            states = states.SetState(actionCtx.Signer, avatarState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
