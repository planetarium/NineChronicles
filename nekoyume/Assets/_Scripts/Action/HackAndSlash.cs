using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Game.Item;
using Nekoyume.State;

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

        protected override IAccountStateDelta ExecuteInternal(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }
            var avatarState = (AvatarState) states.GetState(ctx.Signer);
            if (avatarState == null)
            {
                return SimpleError(ctx, ErrorCode.AvatarNotFound);
            }
            
            var items = avatarState.items.Select(i => i.Item).ToImmutableHashSet();
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
            
            var simulator = new Simulator(ctx.Random, avatarState, Foods, Stage);
            var player = simulator.Simulate();
            avatarState.Update(player);
            avatarState.battleLog = simulator.Log;
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            if (avatarState.worldStage > Stage)
            {
                var ranking = (RankingState) states.GetState(RankingState.Address) ?? new RankingState();
                avatarState.clearedAt = DateTimeOffset.UtcNow;
                ranking.Update(avatarState);
                states = states.SetState(RankingState.Address, ranking);
            }
            return states.SetState(ctx.Signer, avatarState);
        }
    }
}
