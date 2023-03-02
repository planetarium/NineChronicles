using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("charge_action_point")]
    public class ChargeActionPoint0 : GameAction, IChargeActionPointV1
    {
        public Address avatarAddress;

        Address IChargeActionPointV1.AvatarAddress => avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out var _, out var avatarState))
            {
                return states;
            }

            var row = states.GetSheet<MaterialItemSheet>().Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem2(apStone))
            {
                Log.Error("{AddressesHex}Not enough item {ApStone}", addressesHex, apStone);
                return states;
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                return states;
            }

            avatarState.actionPoint = gameConfigState.ActionPointMax;
            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
