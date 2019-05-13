using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        public List<ItemBase> Items;
        public decimal Price;
        
        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["items"] = ByteSerializer.Serialize(Items),
            ["price"] = Price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Items = ByteSerializer.Deserialize<List<ItemBase>>((byte[])plainValue["items"]);
            Price = decimal.Parse(plainValue["price"].ToString());
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer) ?? CreateNovice.CreateContext("dummy", default(Address));
            var shop = ActionManager.instance.shop ?? new Shop();
            var player = new Player(ctx.avatar);
            foreach (var item in Items)
            {
                var owned = player.inventory.items.FirstOrDefault(i => i.Item.Equals(item) && i.Count >= 1);
                if (owned == null)
                {
                    if (actionCtx.Rehearsal)
                    {
                        continue;
                    }
                    throw new InvalidActionException();
                }
                owned.Count--;
                var reservedItem = ItemBase.ItemFactory(owned.Item.Data);
                reservedItem.registeredToShop = true;
                player.inventory.Add(reservedItem);
                ctx.avatar.Update(player);
                shop.Set(actionCtx.Signer, reservedItem);
            }
            ctx.updatedAt = DateTimeOffset.UtcNow;
            states = states.SetState(ActionManager.shopAddress, shop);
            states = states.SetState(actionCtx.Signer, ctx);
            return states;
        }
    }
}
