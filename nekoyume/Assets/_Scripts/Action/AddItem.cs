using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Mail;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_item")]
    public class AddItem : GameAction
    {
        public Guid itemId;
        public Address avatarAddress;
        public bool canceled;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            var mail = avatarState.mailBox.OfType<AttachmentMail>()
                .FirstOrDefault(i => i.attachment.itemUsable?.ItemId == itemId && i.New);
            if (mail is null)
                return states;

            mail.New = false;
            avatarState.UpdateFromAddItem(mail.attachment.itemUsable, canceled);
            avatarState.blockIndex = ctx.BlockIndex;
            states = states.SetState(avatarAddress, avatarState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["itemId"] = itemId.Serialize(),
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["canceled"] = new Bencodex.Types.Boolean(canceled),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            canceled = ((Bencodex.Types.Boolean) plainValue["canceled"]).Value;
        }
    }
}
