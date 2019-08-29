using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_item")]
    public class AddItem : GameAction
    {
        public Guid itemId;
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
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

            var mail = avatarState.mailBox.FirstOrDefault(i => i.attachment.itemUsable?.ItemId == itemId && i.New);
            if (mail is null)
                return states;

            mail.New = false;
            avatarState.inventory.AddNonFungibleItem(mail.attachment.itemUsable);
            avatarState.BlockIndex = ctx.BlockIndex;
            states = states.SetState(avatarAddress, avatarState);
            return states;
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["itemId"] = itemId.ToString(),
            ["avatarAddress"] = avatarAddress.ToByteArray(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            itemId = new Guid((string) plainValue["itemId"]);
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
        }
    }
}
