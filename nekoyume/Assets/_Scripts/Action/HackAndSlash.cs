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
        public List<Food> Foods;

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["equipments"] = ByteSerializer.Serialize(Equipments),
                ["foods"] = ByteSerializer.Serialize(Foods),
            }.ToImmutableDictionary();


        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Equipments = ByteSerializer.Deserialize<List<Equipment>>((byte[]) plainValue["equipments"]);
            Foods = ByteSerializer.Deserialize<List<Food>>((byte[]) plainValue["foods"]);
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                if (ctx == null)
                {
                    ctx = CreateNovice.CreateContext("dummy");
                }

                return states.SetState(actionCtx.Signer, ctx);
            }
            var items = ctx.avatar.Items.Select(i => i.Item).ToImmutableHashSet();
            var currentEquipments = items.OfType<Equipment>().ToImmutableHashSet();
            if (Equipments.Count > 0)
            {
                foreach (var equipment in Equipments)
                {
                    if (!currentEquipments.Contains(equipment))
                    {
                        throw new InvalidActionException();
                    }

                    var equip = currentEquipments.First(e => e.Data.id == equipment.Data.id);
                    equip.Equip();
                }
            }
            else
            {
                foreach (var equipment in currentEquipments)
                {
                    equipment.Unequip();
                }
            }

            if (Foods.Count > 0)
            {
                var currentFoods = items.OfType<Food>().ToImmutableHashSet();
                foreach (var food in Foods)
                {
                    if (!currentFoods.Contains(food))
                    {
                        Foods.Remove(food);
                    }
                }
            }

            var simulator = new Simulator(actionCtx.Random, ctx.avatar, Foods);
            var player = simulator.Simulate();
            ctx.avatar.Update(player);
            ctx.battleLog = simulator.Log;
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
