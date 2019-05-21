using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        [Serializable]
        public class ResultModel : GameActionResult
        {
            public string owner;
            public ShopItem shopItem;
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
                states = states.SetState(ActionManager.ShopAddress, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var shop = (Shop) states.GetState(ActionManager.ShopAddress) ?? new Shop();

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            Inventory.InventoryItem target = null;
            foreach (var item in ctx.avatar.Items)
            {
                if (item.Item.Data.id != itemId ||
                    item.Count == 0)
                {
                    continue;
                }

                target = item;
                if (target.Count < count)
                {
                    return SimpleError(actionCtx, ctx, GameActionResult.ErrorCode.SellItemCountNotEnoughInInventory);
                }
                target.Count -= count;
            }

            // 인벤토리에 판매할 아이템이 없는 경우.
            if (ReferenceEquals(target, null))
            {
                return SimpleError(actionCtx, ctx, GameActionResult.ErrorCode.SellItemNotFoundInInventory);
            }

            // 인벤토리에서 판매할 아이템을 뺀 후에 수량이 0일 경우.
            if (target.Count == 0)
            {
                ctx.avatar.Items.Remove(target);
            }

            // 상점에 아이템을 등록한다.
            var shopItem = new ShopItem {item = target.Item, count = count, price = price};
            shopItem = shop.Register(actionCtx.Signer, shopItem);

            ctx.updatedAt = DateTimeOffset.UtcNow;
            ctx.SetGameActionResult(new ResultModel
            {
                errorCode = GameActionResult.ErrorCode.Success,
                owner = actionCtx.Signer.ToString(),
                shopItem = shopItem
            });

            states = states.SetState(ActionManager.ShopAddress, shop);
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
