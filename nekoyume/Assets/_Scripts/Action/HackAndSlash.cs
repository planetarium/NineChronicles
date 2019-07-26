using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
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
        public Address avatarAddress;

        protected override IImmutableDictionary<string, object> PlainValueInternal =>
            new Dictionary<string, object>
            {
                ["equipments"] = ByteSerializer.Serialize(equipments),
                ["foods"] = ByteSerializer.Serialize(foods),
                ["stage"] = ByteSerializer.Serialize(stage),
                ["avatarAddress"] = avatarAddress.ToByteArray(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            equipments = ByteSerializer.Deserialize<List<Equipment>>((byte[]) plainValue["equipments"]);
            foods = ByteSerializer.Deserialize<List<Food>>((byte[]) plainValue["foods"]);
            stage = ByteSerializer.Deserialize<int>((byte[]) plainValue["stage"]);
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var agentState = (AgentState) states.GetState(ctx.Signer);
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState == null)
            {
                return states;
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
                    return states;
                }
                
                ((Equipment) outNonFungibleItem).Equip();
            }
            
            var simulator = new Simulator(ctx.Random, avatarState, foods, stage);
            var player = simulator.Simulate();
            avatarState.Update(player, simulator.rewards);
            avatarState.battleLog = simulator.Log;
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            if (avatarState.worldStage > stage)
            {
                var ranking = (RankingState) states.GetState(RankingState.Address);
                avatarState.clearedAt = DateTimeOffset.UtcNow;
                ranking.Update(avatarState);
                states = states.SetState(RankingState.Address, ranking);
            }

            states = states.SetState(avatarAddress, avatarState);
            return states.SetState(ctx.Signer, agentState);
        }
    }
}
