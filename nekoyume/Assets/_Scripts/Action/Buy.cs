using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("buy")]
    public class Buy : GameAction
    {
        [Serializable]
        public class ResultModel : GameActionResult
        {
            public string owner;
            public ShopItem shopItem;
        }

        public string owner;
        public Guid productId;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["owner"] = owner,
            ["productId"] = productId,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            owner = (string) plainValue["owner"];
            productId = new Guid((string) plainValue["productId"]);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(ActionManager.ShopAddress, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var shop = (Shop) states.GetState(ActionManager.ShopAddress);
            
            try
            {
                // 상점에서 구매할 아이템을 찾는다.
                var target = shop.Find(owner, productId);
                
                // 돈은 있냐?
                if (ctx.gold < target.Value.price)
                {
                    return SimpleError(actionCtx, ctx, GameActionResult.ErrorCode.BuyGoldNotEnough);
                }
                
                // 상점에서 구매할 아이템을 제거한다.
                if (!shop.Unregister(target.Key, target.Value))
                {
                    return SimpleError(actionCtx, ctx, GameActionResult.ErrorCode.UnexpectedInternalAction);
                }
                
                // 돈을 차감한다.
                ctx.gold -= target.Value.price;
                
                // 인벤토리에 구매한 아이템을 넣는다.
                ctx.avatar.AddEquipmentItemToItems(target.Value.item.Data.id, target.Value.count);
                
                ctx.updatedAt = DateTimeOffset.UtcNow;
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.Success,
                    owner = target.Key,
                    shopItem = target.Value,
                });

                states = states.SetState(ActionManager.ShopAddress, shop);
                return states.SetState(actionCtx.Signer, ctx);
            }
            catch
            {
                return SimpleError(actionCtx, ctx, GameActionResult.ErrorCode.UnexpectedInternalAction);
            }
        }
    }
}
