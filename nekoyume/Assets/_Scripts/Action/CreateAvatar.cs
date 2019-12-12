using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("create_avatar")]
    public class CreateAvatar : GameAction
    {
        public Address avatarAddress;
        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["index"] = (Integer) index,
            ["hair"] = (Integer) hair,
            ["lens"] = (Integer) lens,
            ["ear"] = (Integer) ear,
            ["tail"] = (Integer) tail,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            index = (int) ((Integer) plainValue["index"]).Value;
            hair = (int) ((Integer) plainValue["hair"]).Value;
            lens = (int) ((Integer) plainValue["lens"]).Value;
            ear = (int) ((Integer) plainValue["ear"]).Value;
            tail = (int) ((Integer) plainValue["tail"]).Value;
            name = (Text) plainValue["name"];
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ctx.Signer, MarkChanged);
                return states.SetState(avatarAddress, MarkChanged);
            }

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                return states;
            }

            AgentState agentState = states.GetAgentState(ctx.Signer) ?? new AgentState(ctx.Signer);
            AvatarState avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                return states;
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                return states;
            }

            Debug.Log($"Execute CreateAvatar. player : `{avatarAddress}` " +
                      $"node : `{States.Instance?.AgentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.Value?.address}`");

            agentState.avatarAddresses.Add(index, avatarAddress);
            
            // Avoid NullReferenceException in test
            avatarState = CreateAvatarState(name, avatarAddress, ctx);

            if (hair < 0) hair = 0;
            if (lens < 0) lens = 0;
            if (ear < 0) ear = 0;
            if (tail < 0) tail = 0;

            avatarState.Customize(hair, lens, ear, tail);

            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
        }

        private static AvatarState CreateAvatarState(string name, Address avatarAddress, IActionContext ctx)
        {
            var tableSheets = TableSheets.FromActionContext(ctx);
            var avatarState = new AvatarState(
                avatarAddress, 
                ctx.Signer, 
                ctx.BlockIndex, 
                tableSheets.WorldSheet,
                tableSheets.QuestSheet,
                name
            );
#if UNITY_EDITOR
            AddItemsForTest(avatarState, ctx.Random, tableSheets);
#endif
            return avatarState;
        }

        private static void AddItemsForTest(AvatarState avatarState, IRandom random, TableSheets tableSheets)
        {
            foreach (var row in tableSheets.MaterialItemSheet)
            {
                avatarState.inventory.AddItem(ItemFactory.Create(row, default), 10);
            }

            foreach (var pair in tableSheets.EquipmentItemSheet.Where(pair =>
                pair.Value.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem((ItemUsable) ItemFactory.Create(pair.Value, itemId));
            }
        }
    }
}
