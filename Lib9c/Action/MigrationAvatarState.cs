using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/618
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("migration_avatar_state")]
    public class MigrationAvatarState : GameAction, IMigrationAvatarStateV1
    {
        public List<Dictionary> avatarStates;

        IEnumerable<IValue> IMigrationAvatarStateV1.AvatarStates => avatarStates;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            CheckPermission(context);

            foreach (var rawAvatar in avatarStates)
            {
                var v1 = new AvatarState(rawAvatar);
                var inventoryAddress = v1.address.Derive(LegacyInventoryKey);
                var worldInformationAddress = v1.address.Derive(LegacyWorldInformationKey);
                var questListAddress = v1.address.Derive(LegacyQuestListKey);
                if (states.GetState(inventoryAddress) is null)
                {
                    states = states.SetState(inventoryAddress, v1.inventory.Serialize());
                }
                if (states.GetState(worldInformationAddress) is null)
                {
                    states = states.SetState(worldInformationAddress, v1.worldInformation.Serialize());
                }
                if (states.GetState(questListAddress) is null)
                {
                    states = states.SetState(questListAddress, v1.questList.Serialize());
                }

                var v2 = states.GetAvatarStateV2(v1.address);
                if (v2.inventory is null || v2.worldInformation is null || v2.questList is null)
                {
                    throw new FailedLoadStateException(v1.address.ToHex());
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["a"] = avatarStates.Serialize(),
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarStates = plainValue["a"].ToList(i => (Dictionary)i);
        }
    }
}
