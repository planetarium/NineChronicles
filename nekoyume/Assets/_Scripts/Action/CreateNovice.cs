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
        
        public Address avatarAddress;
        public string name;
        
        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>()
        {
            ["avatarAddress"] = avatarAddress.ToByteArray(),
            ["name"] = name,
        }.ToImmutableDictionary();
        
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            name = (string) plainValue["name"];
            avatarAddress = new Address((byte[])plainValue["avatarAddress"]);
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            if (actionCtx.Rehearsal)
            {
                return states.SetState(actionCtx.Signer, MarkChanged);
            }
            
            var ctx = (AvatarState)states.GetState(actionCtx.Signer) ?? CreateState(name, avatarAddress);

            return states.SetState(actionCtx.Signer, ctx);
        }
        
        private static AvatarState CreateState(string name, Address avatarAddress)
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
