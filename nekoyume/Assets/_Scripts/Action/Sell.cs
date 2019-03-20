using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : ActionBase
    {
        public List<ItemBase> Items;
        public decimal Price;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Items = ByteSerializer.Deserialize<List<ItemBase>>((byte[])plainValue["items"]);
            Price = decimal.Parse(plainValue["price"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer) ?? CreateNovice.CreateContext("dummy");
            var shop = ActionManager.Instance.shop ?? new Shop();
            var player = new Player(ctx.avatar);
            foreach (var item in Items)
            {
                var owned = player.inventory.items.FirstOrDefault(i => i.Item.Equals(item) && i.Count >= 1);
                if (owned == null)
                {
                    if (actionCtx.BlockIndex < 1)
                    {
                        // While a new transaction is being created, every action
                        // executes once for "rehearsal mode" (i.e., dry-run).
                        // Since during this mode any states are not provided,
                        // we work around this by checking if the block index is 0.
                        // ("Rehearsal model" sets .BlockIndex to 0.)
                        // FIXME: Detecting rehearsal model by checking .BlockIndex
                        // seems unstable.  Libplanet needs to add a Boolean property
                        // for this specific purpose, like .Rehearsal.
                        continue;
                    }
                    throw new InvalidActionException();
                }
                owned.Count--;
                var reservedItem = ItemBase.ItemFactory(owned.Item.Data);
                reservedItem.reserved = true;
                player.inventory.Add(reservedItem);
                ctx.avatar.Update(player);
                shop.Set(actionCtx.Signer, reservedItem);
            }
            states = states.SetState(ActionManager.shopAddress, shop);
            states = states.SetState(actionCtx.Signer, ctx);
            return states;
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["items"] = ByteSerializer.Serialize(Items),
            ["price"] = Price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();
    }
}
