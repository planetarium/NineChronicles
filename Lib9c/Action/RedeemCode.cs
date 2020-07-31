using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Crypto;
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
        public PublicKey code;
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

            int redeemId;
            try
            {
                redeemId = redeemState.Redeem(code, avatarAddress);
            }
            catch (InvalidRedeemCodeException)
            {
                Log.Error("Invalid Code");
                throw;
            }
            catch (DuplicateRedeemException e)
            {
                Log.Warning(e.Message);
                throw;
            }

            var tableSheets = TableSheets.FromActionContext(context);
            var row = tableSheets.RedeemRewardSheet.Values.First(r => r.Id == redeemId);
            var rewards = row.Rewards;
            var materialRow = tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
            var chest = ItemFactory.CreateChest(materialRow, rewards);
            avatarState.inventory.AddItem(chest, 1);
            states = states.SetState(avatarAddress, avatarState.Serialize());
            states = states.SetState(RedeemCodeState.Address, redeemState.Serialize());
            states = states.SetState(context.Signer, agentState.Serialize());
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
            code = plainValue["code"].ToPublicKey();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
