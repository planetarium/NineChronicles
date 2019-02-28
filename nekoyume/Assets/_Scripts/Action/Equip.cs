using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Action;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("equip")]
    public class Equip : ActionBase
    {
        public List<Inventory.InventoryItem> items;

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["items"] = ByteSerializer.Serialize(items),
            }.ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            items = ByteSerializer.Deserialize<List<Inventory.InventoryItem>>((byte[]) plainValue["items"]);
        }

        public override AddressStateMap Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var to = actionCtx.To;
            var ctx = (Context) states.GetValueOrDefault(to);
            ctx.avatar.Items = items;
            return (AddressStateMap) states.SetItem(to, ctx);
        }
    }
}
