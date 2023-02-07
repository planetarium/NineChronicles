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
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/430
    /// Updated at https://github.com/planetarium/lib9c/pull/474
    /// Updated at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/861
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("charge_action_point3")]
    public class ChargeActionPoint : GameAction, IChargeActionPointV1
    {
        public Address avatarAddress;

        Address IChargeActionPointV1.AvatarAddress => avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);

            if (context.Rehearsal)
            {
                return states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(avatarAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ChargeActionPoint exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var row = states.GetSheet<MaterialItemSheet>().Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            if (avatarState.actionPoint == gameConfigState.ActionPointMax)
            {
                throw new ActionPointExceededException();
            }

            avatarState.actionPoint = gameConfigState.ActionPointMax;
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ChargeActionPoint Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
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
