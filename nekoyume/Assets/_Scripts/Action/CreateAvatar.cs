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
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("create_avatar")]
    public class CreateAvatar : GameAction
    {
        public Address avatarAddress;
        public int index;
        public string name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["index"] = (Integer) index,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            index = (int) ((Integer) plainValue["index"]).Value;
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
            if (!states.TryGetState(DailyBlockState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            var dailyBlockState = new DailyBlockState(d);
            // Avoid NullReferenceException in test
            var nextBlockIndex = dailyBlockState?.nextBlockIndex ?? DailyBlockState.UpdateInterval;
            avatarState = CreateAvatarState(name, avatarAddress, ctx, nextBlockIndex);

            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
        }

        private static AvatarState CreateAvatarState(string name, Address avatarAddress, IActionContext ctx, long index)
        {
            var avatarState = new AvatarState(avatarAddress, ctx.Signer, ctx.BlockIndex, index, name);
#if UNITY_EDITOR
            AddItemsForTest(avatarState, ctx.Random);
#endif
            return avatarState;
        }

        private static void AddItemsForTest(AvatarState avatarState, IRandom random)
        {
            foreach (var row in Game.Game.instance.TableSheets.MaterialItemSheet)
            {
                avatarState.inventory.AddItem(ItemFactory.Create(row, default), 10);
            }

            foreach (var pair in Game.Game.instance.TableSheets.EquipmentItemSheet.Where(pair =>
                pair.Value.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem((ItemUsable) ItemFactory.Create(pair.Value, itemId));
            }
        }
    }
}
