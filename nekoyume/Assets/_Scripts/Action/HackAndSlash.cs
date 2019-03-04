using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : ActionBase
    {
        public List<Equipment> Equipments;

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["equipments"] = ByteSerializer.Serialize(Equipments),
            }.ToImmutableDictionary();


        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Equipments = ByteSerializer.Deserialize<List<Equipment>>((byte[]) plainValue["equipments"]);
        }

        public override AddressStateMap Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var to = actionCtx.To;
            var ctx = (Context) states.GetValueOrDefault(to);
            if (ctx.avatar.Dead)
            {
                throw new InvalidActionException();
            }
            var current = ctx.avatar.Items.Select(i => i.Item).OfType<Equipment>().ToArray();
            if (Equipments.Count > 0)
            {
                foreach (var equipment in Equipments)
                {
                    if (!current.Contains(equipment))
                    {
                        throw new InvalidActionException();
                    }

                    var equip = current.First(e => e.Data.Id == equipment.Data.Id);
                    equip.IsEquipped = equipment.IsEquipped;
                }
            }
            else
            {
                foreach (var equipment in current)
                {
                    equipment.Unequip();
                }
            }
            var simulator = new Simulator(0, ctx.avatar);
            var player = simulator.Simulate();
            ctx.avatar.Update(player);
            ctx.battleLog = simulator.Log;
            return (AddressStateMap) states.SetItem(to, ctx);
        }
    }
}
