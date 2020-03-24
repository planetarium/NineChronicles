using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("rapid_combination")]
    public class RapidCombination : GameAction
    {
        public Address avatarAddress;
        public int slotIndex;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress,
                out var agentState, out var avatarState))
            {
                return states;
            }

            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState?.Result is null || slotState.UnlockBlockIndex <= context.BlockIndex)
            {
                return states;
            }

            var cost = slotState.Result.itemUsable.RequiredBlockIndex - context.BlockIndex;
            if (cost < 0)
            {
                return states;
            }

            if (!agentState.PurchaseGold(cost))
            {
                return states;
            }

            slotState.Update(context.BlockIndex);
            avatarState.UpdateFromRapidCombination(
                ((CombinationConsumable.ResultModel) slotState.Result),
                context.BlockIndex
            );
            return states
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["slotIndex"] = slotIndex.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            slotIndex = plainValue["slotIndex"].ToInteger();
        }
    }
}
