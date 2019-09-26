using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("create_avatar")]
    public class CreateAvatar : GameAction
    {
        public Address avatarAddress;
        public int index;
        public string name;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>()
        {
            ["avatarAddress"] = avatarAddress.ToByteArray(),
            ["index"] = index.ToString(),
            ["name"] = name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
            index = int.Parse(plainValue["index"].ToString());
            name = (string) plainValue["name"];
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

            var agentState = (AgentState) states.GetState(ctx.Signer) ?? new AgentState(ctx.Signer);
            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState != null)
            {
                return states;
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                return states;
            }

            Debug.Log($"Execute CreateAvatar. player : `{avatarAddress}` " +
                      $"node : `{States.Instance?.agentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.currentAvatarState?.Value?.address}`");

            agentState.avatarAddresses.Add(index, avatarAddress);
            avatarState = CreateAvatarState(name, avatarAddress, ctx);

            states = states.SetState(ctx.Signer, agentState);
            return states.SetState(avatarAddress, avatarState);
        }

        private static AvatarState CreateAvatarState(string name, Address avatarAddress, IActionContext ctx)
        {
            var avatarState = new AvatarState(avatarAddress, ctx.Signer, ctx.BlockIndex, name);
#if UNITY_EDITOR
            AddItemsForTest(avatarState, ctx.Random);
#endif
            return avatarState;
        }

        private static void AddItemsForTest(AvatarState avatarState, IRandom random)
        {
            foreach (var row in Game.Game.instance.TableSheets.MaterialItemSheet)
            {
                avatarState.inventory.AddFungibleItem(ItemFactory.Create(row, default), 10);
            }

            foreach (var pair in Game.Game.instance.TableSheets.EquipmentItemSheet.Where(pair =>
                pair.Value.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddNonFungibleItem((ItemUsable) ItemFactory.Create(pair.Value, itemId));
            }
        }
    }
}
