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
        public class ResultModel : GameActionResult
        {
            public int id;
        }

        private int _itemID;
        private int _count;
        private decimal _price;

        public SellRenew(int itemID, int count, decimal price)
        {
            _itemID = itemID;
            _count = count;
            _price = price;
        }
        
        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["item"] = ByteSerializer.Serialize(_itemID),
            ["price"] = _price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            _itemID = ByteSerializer.Deserialize<int>((byte[])plainValue["itemID"]);
            _price = decimal.Parse(plainValue["price"].ToString());
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
            
            var shop = ActionManager.instance.shop ?? new Shop();
            var player = new Player(ctx.avatar);

            Inventory.InventoryItem input = null;
            foreach (var item in player.inventory.items)
            {
                if (item.Item.Data.id != _itemID ||
                    item.Count == 0)
                {
                    continue;
                }

                input = item;
                input.Count--;
            }

            if (ReferenceEquals(input, null))
            {
                ctx.SetGameActionResult(new ResultModel
                {
                    errorCode = GameActionResult.ErrorCode.SellItemNotFound,
                    id = _itemID
                });
                
                return states.SetState(actionCtx.Signer, ctx);
            }
            
            shop.Set(actionCtx.Signer, ItemBase.ItemFactory(input.Item.Data));
            ctx.avatar.Update(player);
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
