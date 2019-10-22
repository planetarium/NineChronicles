using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
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

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["equipments"] = new Bencodex.Types.List(equipments.Select(e => e.Serialize())),
                ["foods"] = new Bencodex.Types.List(foods.Select(e => e.Serialize())),
                ["stage"] = (Integer) stage,
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();


        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            equipments = ((Bencodex.Types.List) plainValue["equipments"]).Select(
                e => (Equipment) ItemFactory.Deserialize((Bencodex.Types.Dictionary) e)
            ).ToList();
            foods = ((Bencodex.Types.List) plainValue["foods"]).Select(
                e => (Consumable) ItemFactory.Deserialize((Bencodex.Types.Dictionary) e)
            ).ToList();
            stage = (int) ((Integer) plainValue["stage"]).Value;
            avatarAddress = plainValue["avatarAddress"].ToAddress();
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

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            if (avatarState.actionPoint < GameConfig.HackAndSlashCost)
            {
                return states;
            }

            avatarState.actionPoint -= GameConfig.HackAndSlashCost;
            
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
                      $"player : `{avatarAddress}` node : `{States.Instance?.AgentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.Value?.address}`");
            avatarState.Update(player, simulator.rewards);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            if (avatarState.worldStage > stage)
            {
                if (!states.TryGetState(RankingState.Address, out Bencodex.Types.Dictionary d))
                {
                    return states;
                }

                var ranking = new RankingState(d);
                avatarState.clearedAt = DateTimeOffset.UtcNow;
                ranking.Update(avatarState);
                states = states.SetState(RankingState.Address, ranking.Serialize());
            }

            states = states.SetState(avatarAddress, avatarState.Serialize());
            Result = simulator.Log;
            return states.SetState(ctx.Signer, agentState.Serialize());
        }
    }
}
