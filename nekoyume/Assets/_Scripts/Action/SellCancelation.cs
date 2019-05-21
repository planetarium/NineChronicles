using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("sell_cancelation")]
    public class SellCancelation : GameAction
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

            var shop = (Shop) states.GetState(ActionManager.ShopAddress) ?? new Shop();

            try
            {
                // 상점에서 아이템을 빼온다.
                var target = shop.Unregister(owner, productId);

                // 인벤토리에 아이템을 넣는다.
                ctx.avatar.AddEquipmentItemToItems(target.item.Data.id, target.count);

                ctx.updatedAt = DateTimeOffset.UtcNow;
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.Success,
                    owner = owner,
                    shopItem = target
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
