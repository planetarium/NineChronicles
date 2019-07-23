using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.State;

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

            agentState.avatarAddresses.Add(index, avatarAddress);
            avatarState = CreateAvatarState(name, avatarAddress, ctx.Signer);

            states = states.SetState(ctx.Signer, agentState);
            return states.SetState(avatarAddress, avatarState);
        }

        private static AvatarState CreateAvatarState(string name, Address avatarAddress, Address agentAddress)
        {
            var avatarState = new AvatarState(avatarAddress, agentAddress, name);
            foreach (var pair in Tables.instance.Item)
            {
                avatarState.inventory.AddFungibleItem(ItemBase.ItemFactory(pair.Value));
            }
            foreach (var pair in Tables.instance.ItemEquipment.Where(e => e.Value.id > 10100000))
            {
                avatarState.inventory.AddFungibleItem(ItemBase.ItemFactory(pair.Value));
            }
            return avatarState;
        }
    }
}
