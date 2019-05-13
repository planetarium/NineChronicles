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
    [ActionType("sell_renew")]
    public class SellRenew : GameAction
    {
        [Serializable]
        public class ResultModel : GameActionResult
        {
            public byte[] owner;
            public int id;
            public int count;
            public decimal price;
        }

        private int _itemID;
        private int _count;
        private decimal _price;

        public SellRenew()
        {
            
        }
        
        public SellRenew(int itemID, int count, decimal price)
        {
            _itemID = itemID;
            _count = count;
            _price = price;
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["_itemID"] = _itemID.ToString(),
            ["_count"] = _count.ToString(),
            ["_price"] = _price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            _itemID = int.Parse(plainValue["_itemID"].ToString());
            _count = int.Parse(plainValue["_count"].ToString());
            _price = decimal.Parse(plainValue["_price"].ToString());
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

            var shop = ActionManager.instance.shop.Value ?? new Shop();

            Inventory.InventoryItem target = null;
            foreach (var item in ctx.avatar.Items)
            {
                if (item.Item.Data.id != _itemID ||
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
                    owner = actionCtx.Signer.ToByteArray(),
                    id = _itemID,
                    count = _count,
                    price = _price
                });

                return states.SetState(actionCtx.Signer, ctx);
            }

            if (target.Count == 0)
            {
                ctx.avatar.Items.Remove(target);
            }
            
//            var shopItem = new ShopItem();
//            shopItem.owner.Value = actionCtx.Signer.ToByteArray();
//            shopItem.item.Value = 

            shop.Set(actionCtx.Signer, ItemBase.ItemFactory(target.Item.Data));
            ctx.updatedAt = DateTimeOffset.UtcNow;
            ctx.SetGameActionResult(new ResultModel
            {
                errorCode = GameActionResult.ErrorCode.Success,
                id = _itemID
            });

            states = states.SetState(ActionManager.shopAddress, shop);
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
