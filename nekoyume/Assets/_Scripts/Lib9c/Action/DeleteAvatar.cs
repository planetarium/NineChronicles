using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("delete_avatar")]
    public class DeleteAvatar : GameAction
    {
        public int index;
        public Address avatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["index"] = (Integer) index,
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            index = (int) ((Integer) plainValue["index"]).Value;
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ctx.Signer, MarkChanged);
                return states.SetState(avatarAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            if (!agentState.avatarAddresses.ContainsKey(index))
            {
                return states;
            }

            if (!agentState.avatarAddresses[index].Equals(avatarAddress))
            {
                return states;
            }

            agentState.avatarAddresses.Remove(index);

            var deletedAvatarState = new DeletedAvatarState(avatarState, DateTimeOffset.UtcNow)
            {
                blockIndex = ctx.BlockIndex,
            };

            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(avatarAddress, deletedAvatarState.Serialize());
        }
    }
}
