using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("create_avatar")]
    public class CreateAvatar : GameAction
    {
        private const int DefaultItemEquipmentSetId = 1;

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
            avatarAddress = new Address((byte[])plainValue["avatarAddress"]);
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
            
            var agentState = (AgentState)states.GetState(ctx.Signer) ?? new AgentState(ctx.Signer);            
            var avatarState = (AvatarState)states.GetState(avatarAddress);
            if (avatarState != null)
            {
                return SimpleError(ctx, ErrorCode.CreateAvatarAlreadyExistAvatarAddress);
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                return SimpleError(ctx, ErrorCode.CreateAvatarAlreadyExistKeyAvatarAddress);
            }

            agentState.avatarAddresses.Add(index, avatarAddress);
            avatarState = CreateAvatarState(name, avatarAddress);

            states = states.SetState(ctx.Signer, agentState);
            return states.SetState(avatarAddress, avatarState);
        }
        
        private static AvatarState CreateAvatarState(string name, Address avatarAddress)
        {
            var avatarState = new AvatarState(avatarAddress, name);
            var table = Tables.instance.ItemEquipment;
            foreach (var data in table.Select(i => i.Value).Where(e => e.setId == DefaultItemEquipmentSetId))
            {
                var equipment = (ItemUsable) ItemBase.ItemFactory(data);
                avatarState.inventory.AddUnfungibleItem(equipment);
            }
            return avatarState;
        }
    }
}
