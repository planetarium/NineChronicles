using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.State;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Lib9c.DevExtensions.Action.Stage
{
    [Serializable]
    [ActionType("clear_stage")]
    public class ClearStage : GameAction
    {
        public Address AvatarAddress { get; set; }
        public int TargetStage { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            var worldInformation = new WorldInformation(
                context.BlockIndex,
                states.GetSheet<WorldSheet>(),
                TargetStage
            );
            return states.SetState(AvatarAddress.Derive(SerializeKeys.LegacyWorldInformationKey),
                worldInformation.Serialize()
            );
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["targetStage"] = TargetStage.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            TargetStage = plainValue["targetStage"].ToInteger();
        }
    }
}
