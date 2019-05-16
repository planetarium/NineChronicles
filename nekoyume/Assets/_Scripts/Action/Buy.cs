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
            public ShopItem shopItem;
        }

        public Guid productId;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["productId"] = productId,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            productId = new Guid((string) plainValue["productId"]);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(ActionManager.shopAddress, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var shop = (Shop) states.GetState(ActionManager.shopAddress);
            
            try
            {
                var target = shop.Unregister(productId);
                
                ctx.updatedAt = DateTimeOffset.UtcNow;
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.Success,
                    shopItem = target.Value,
                });

                states = states.SetState(ActionManager.shopAddress, shop);
                return states.SetState(actionCtx.Signer, ctx);
            }
            catch
            {
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.Fail,
                });

                return states.SetState(actionCtx.Signer, ctx);
            }
        }
    }
}
