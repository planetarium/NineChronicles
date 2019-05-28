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
    [ActionType("create_novice")]
    public class CreateNovice : GameAction
    {
        public const int DefaultId = 100010;
        private const int DefaultItemEquipmentSetId = 1;

        public Address agentAddress;
        public int index;
        public string name;
        
        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>()
        {
            ["agentAddress"] = agentAddress.ToByteArray(),
            ["index"] = index.ToString(),
            ["name"] = name,
        }.ToImmutableDictionary();
        
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            agentAddress = new Address((byte[])plainValue["agentAddress"]);
            index = int.Parse(plainValue["index"].ToString());
            name = (string) plainValue["name"];
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(agentAddress, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }
            
            var avatarState = (AvatarState)states.GetState(actionCtx.Signer);
            if (avatarState != null)
            {
                return SimpleError(actionCtx, avatarState, GameActionErrorCode.CreateNoviceAlreadyExistAvatarAddress);
            }

            var agentState = (AgentState)states.GetState(agentAddress);
            agentState.avatarAddresses.Add(index, actionCtx.Signer);
            avatarState = CreateAvatarState(name, actionCtx.Signer);

            states = states.SetState(agentAddress, agentState);
            return states.SetState(actionCtx.Signer, avatarState);
        }
        
        private static AvatarState CreateAvatarState(string name, Address avatarAddress)
        {
            var avatarState = new AvatarState(avatarAddress, name);
            var table = Tables.instance.ItemEquipment;
            foreach (var data in table.Select(i => i.Value).Where(e => e.setId == DefaultItemEquipmentSetId))
            {
                var equipment = ItemBase.ItemFactory(data);
                avatarState.items.Add(new Inventory.InventoryItem(equipment));
            }
            return avatarState;
        }
    }
}
