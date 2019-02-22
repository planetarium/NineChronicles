using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Newtonsoft.Json;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : ActionBase
    {
        public List<ItemBase> Items;
        public decimal Price;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Items = JsonConvert.DeserializeObject<List<ItemBase>>(plainValue["items"].ToString());
            Price = decimal.Parse(plainValue["price"].ToString());
        }

        public override AddressStateMap Execute(IActionContext actionCtx)
        {
            IImmutableDictionary<Address, object> states = actionCtx.PreviousStates;
            var to = actionCtx.To;
            var ctx = (Context) states.GetValueOrDefault(to);
            var shop = ActionManager.Instance.shop ?? new Shop();
            var player = new Player(ctx.avatar);
            foreach (var item in Items)
            {
                var owned = player.inventory.items.FirstOrDefault(i => i.Item.Equals(item) && i.Count >= 1);
                if (owned == null)
                {
                    throw new InvalidActionException();
                }
                owned.Count--;
                var reservedItem = ItemBase.ItemFactory(owned.Item.Data);
                reservedItem.reserved = true;
                player.inventory.Add(reservedItem);
                ctx.avatar.Update(player);
                shop.Set(to, reservedItem);
            }
            states = states.SetItem(ActionManager.shopAddress, shop);
            states = states.SetItem(to, ctx);
            return (AddressStateMap) states;
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["items"] = JsonConvert.SerializeObject(Items),
            ["price"] = Price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();
    }
}
