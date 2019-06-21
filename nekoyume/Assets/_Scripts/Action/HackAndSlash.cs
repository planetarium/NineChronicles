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
        public List<Equipment> equipments;
        public List<Food> foods;
        public int stage;

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["equipments"] = ByteSerializer.Serialize(equipments),
                ["foods"] = ByteSerializer.Serialize(foods),
                ["stage"] = ByteSerializer.Serialize(stage),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            equipments = ByteSerializer.Deserialize<List<Equipment>>((byte[]) plainValue["equipments"]);
            foods = ByteSerializer.Deserialize<List<Food>>((byte[]) plainValue["foods"]);
            stage = ByteSerializer.Deserialize<int>((byte[]) plainValue["stage"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
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
            
            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .ToImmutableHashSet();
            foreach (var equipment in inventoryEquipments)
            {
                equipment.Unequip();
            }

            foreach (var equipment in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(equipment, out ItemUsable outNonFungibleItem))
                {
                    return SimpleError(ctx, ErrorCode.HackAndSlashNotFoundEquipment);
                }
                
                ((Equipment) outNonFungibleItem).Equip();
            }
            
            var simulator = new Simulator(ctx.Random, avatarState, foods, stage);
            var player = simulator.Simulate();
            avatarState.Update(player);
            avatarState.battleLog = simulator.Log;
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            if (avatarState.worldStage > stage)
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
