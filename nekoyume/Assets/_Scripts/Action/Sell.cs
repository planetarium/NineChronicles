using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        [Serializable]
        public class ResultModel : GameActionResult
        {
            public int itemId;
            public int count;
            public decimal price;
            public string productId;
        }

        public int itemId;
        public int count;
        public decimal price;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["itemId"] = itemId.ToString(),
            ["count"] = count.ToString(),
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            itemId = int.Parse(plainValue["itemId"].ToString());
            count = int.Parse(plainValue["count"].ToString());
            price = decimal.Parse(plainValue["price"].ToString());
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

            Inventory.InventoryItem target = null;
            foreach (var item in ctx.avatar.Items)
            {
                if (item.Item.Data.id != itemId ||
                    item.Count == 0)
                {
                    continue;
                }

                target = item;
                target.Count--;
            }

            if (ReferenceEquals(target, null))
            {
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.SellItemNotFoundInInventory,
                });

                return states.SetState(actionCtx.Signer, ctx);
            }

            if (target.Count == 0)
            {
                ctx.avatar.Items.Remove(target);
            }

            var shopItem = new ShopItem {item = target.Item, count = count, price = price};
            var productId = shop.Register(actionCtx.Signer, shopItem);

            ctx.updatedAt = DateTimeOffset.UtcNow;
            ctx.SetGameActionResult(new ResultModel
            {
                errorCode = GameActionResult.ErrorCode.Success,
                productId = productId,
                itemId = itemId,
                count = count,
                price = price,
            });

            states = states.SetState(ActionManager.shopAddress, shop);
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
