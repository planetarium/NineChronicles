using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("redeem_code")]
    public class RedeemCode : GameAction
    {
        public Address code;
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(avatarAddress, MarkChanged);
                states = states.SetState(context.Signer, MarkChanged);
                return states;
            }

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }

            var redeemState = states.GetRedeemCodeState();
            if (redeemState is null)
            {
                return states;
            }

            int itemId;
            try
            {
                itemId = redeemState.Redeem(code, avatarAddress);
            }
            catch (KeyNotFoundException)
            {
                Log.Error("Invalid Code");
                return states;
            }
            catch (InvalidOperationException e)
            {
                Log.Warning(e.Message);
                return states;
            }

            var tableSheets = TableSheets.FromActionContext(context);
            var row = tableSheets.MaterialItemSheet.Values.First(r => r.Id == itemId);
            var material = ItemFactory.CreateMaterial(row);
            avatarState.inventory.AddItem(material);
            states = states.SetState(avatarAddress, avatarState.Serialize());
            states = states.SetState(RedeemCodeState.Address, redeemState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["code"] = code.Serialize(),
                ["avatarAddress"] = avatarAddress.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            code = plainValue["code"].ToAddress();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
