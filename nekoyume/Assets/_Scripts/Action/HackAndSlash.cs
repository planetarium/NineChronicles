using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : GameAction
    {
        public List<Equipment> Equipments;
        public List<Food> Foods;
        public int Stage;

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["equipments"] = ByteSerializer.Serialize(Equipments),
                ["foods"] = ByteSerializer.Serialize(Foods),
                ["stage"] = ByteSerializer.Serialize(Stage),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            Equipments = ByteSerializer.Deserialize<List<Equipment>>((byte[]) plainValue["equipments"]);
            Foods = ByteSerializer.Deserialize<List<Food>>((byte[]) plainValue["foods"]);
            Stage = ByteSerializer.Deserialize<int>((byte[]) plainValue["stage"]);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var ctx = (Context) states.GetState(actionCtx.Signer);
            if (actionCtx.Rehearsal)
            {
                if (ctx == null)
                {
                    ctx = CreateNovice.CreateContext("dummy");
                }
                states = states.SetState(ActionManager.RankingAddress, new RankingBoard());

                return states.SetState(actionCtx.Signer, ctx);
            }
            var items = ctx.avatar.Items.Select(i => i.Item).ToImmutableHashSet();
            var currentEquipments = items.OfType<Equipment>().ToImmutableHashSet();
            foreach (var equipment in currentEquipments)
            {
                equipment.Unequip();
            }

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

            var simulator = new Simulator(actionCtx.Random, ctx.avatar, Foods, Stage);
            var player = simulator.Simulate();
            ctx.avatar.Update(player);
            ctx.battleLog = simulator.Log;
            ctx.updatedAt = DateTimeOffset.UtcNow;
            if (ctx.avatar.WorldStage > Stage)
            {
                var ranking = ActionManager.instance.rankingBoard ?? new RankingBoard();
                ctx.clearedAt = DateTimeOffset.UtcNow;
                ranking.Update(ctx);
                states = states.SetState(ActionManager.RankingAddress, ranking);
            }
            return states.SetState(actionCtx.Signer, ctx);
        }
    }
}
