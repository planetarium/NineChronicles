using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Item;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        [Serializable]
        public class ResultModel : GameActionResult
        {
            public Address owner;
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
            var ctx = (AvatarState) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(AddressBook.Shop, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var shop = (ShopState) states.GetState(AddressBook.Shop) ?? new ShopState();

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
                owner = actionCtx.Signer,
                shopItem = shopItem
            });

            states = states.SetState(AddressBook.Shop, shop);
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
