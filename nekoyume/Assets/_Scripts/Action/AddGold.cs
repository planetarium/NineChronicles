using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Game.Mail;
using Nekoyume.State;

namespace Nekoyume.Action
{
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

            var agentState = (AgentState) states.GetState(agentAddress);
            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState == null)
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

            avatarState.BlockIndex = ctx.BlockIndex;
            states = states.SetState(avatarAddress, avatarState);
            states = states.SetState(agentAddress, agentState);
            return states;
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["agentAddress"] = agentAddress.ToByteArray(),
            ["avatarAddress"] = avatarAddress.ToByteArray()
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            agentAddress = new Address((byte[]) plainValue["agentAddress"]);
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
        }
    }
}
