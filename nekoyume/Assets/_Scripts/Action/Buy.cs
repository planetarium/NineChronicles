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
        public class ResultModel : GameActionResult
        {
            public Address owner;
            public ShopItem shopItem;
        }

        public Address owner;
        public Guid productId;

        public new ResultModel result;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["owner"] = owner.ToByteArray(),
            ["productId"] = productId,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            owner = new Address((byte[]) plainValue["owner"]);
            productId = new Guid((string) plainValue["productId"]);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var avatar = (AvatarState) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(AddressBook.Shop, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var shop = (ShopState) states.GetState(AddressBook.Shop);
            
            try
            {
                // 상점에서 구매할 아이템을 찾는다.
                var target = shop.Find(owner, productId);
                
                // 돈은 있냐?
                if (avatar.gold < target.Value.price)
                {
                    return SimpleError(actionCtx, avatar, GameActionResult.ErrorCode.BuyGoldNotEnough);
                }
                
                // 상점에서 구매할 아이템을 제거한다.
                if (!shop.Unregister(target.Key, target.Value))
                {
                    return SimpleError(actionCtx, avatar, GameActionResult.ErrorCode.UnexpectedInternalAction);
                }
                
                // 구매자의 돈을 차감한다.
                avatar.gold -= target.Value.price;
                
                // 판매자의 돈을 증가한다.
                // ...
                
                // 구매자의 인벤토리에 구매한 아이템을 넣는다.
                avatar.avatar.AddEquipmentItemToItems(target.Value.item.Data.id, target.Value.count);
                
                avatar.updatedAt = DateTimeOffset.UtcNow;
                result = new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.Success,
                    owner = target.Key,
                    shopItem = target.Value,
                };

                states = states.SetState(AddressBook.Shop, shop);
                return states.SetState(actionCtx.Signer, avatar);
            }
            catch
            {
                return SimpleError(actionCtx, avatar, GameActionResult.ErrorCode.UnexpectedInternalAction);
            }
        }
    }
}
