using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : GameAction
    {
        public List<Equipment> equipments;
        public List<Consumable> foods;
        public int stage;
        public Address avatarAddress;
        public BattleLog Result { get; private set; }
        public const int RequiredPoint = 5;

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
            foods = ByteSerializer.Deserialize<List<Consumable>>((byte[]) plainValue["foods"]);
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

            if (avatarState.actionPoint < RequiredPoint)
            {
                return states;
            }

            avatarState.actionPoint -= RequiredPoint;
            
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
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }
            
            var simulator = new Simulator(ctx.Random, avatarState, foods, stage);
            var player = simulator.Simulate();
            Debug.Log($"Execute HackAndSlash. stage: {stage} result: {simulator.Log?.result} " +
                      $"player : `{avatarAddress}` node : `{States.Instance?.agentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.currentAvatarState?.Value?.address}`");
            avatarState.Update(player, simulator.rewards);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            if (avatarState.worldStage > stage)
            {
                var ranking = (RankingState) states.GetState(RankingState.Address);
                avatarState.clearedAt = DateTimeOffset.UtcNow;
                ranking.Update(avatarState);
                states = states.SetState(RankingState.Address, ranking);
            }

            states = states.SetState(avatarAddress, avatarState);
            Result = simulator.Log;
            return states.SetState(ctx.Signer, agentState);
        }
    }
}
