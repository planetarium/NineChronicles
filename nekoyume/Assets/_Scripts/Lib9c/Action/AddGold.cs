using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_gold")]
    public class AddGold : GameAction
    {
        public Address agentAddress;
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(avatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(agentAddress, avatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            var mail = avatarState.mailBox.OfType<SellerMail>()
                .FirstOrDefault(i => i.New);
            if (mail is null)
                return states;

            mail.New = false;
            var attachment = (Buy.SellerResult) mail.attachment;
            var gold = attachment.gold;
            agentState.gold += gold;

            avatarState.blockIndex = ctx.BlockIndex;
            return states
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(agentAddress, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["agentAddress"] = agentAddress.Serialize(),
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            agentAddress = plainValue["agentAddress"].ToAddress();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
