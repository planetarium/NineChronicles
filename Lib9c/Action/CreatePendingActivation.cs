using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("create_pending_activation")]
    public class CreatePendingActivation : ActionBase
    {
        public PendingActivationState PendingActivation { get; private set; }

        public override IValue PlainValue
            => new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"pending_activation",
                        PendingActivation.Serialize()
                    ),
                }
            );

        public CreatePendingActivation()
        {
        }

        public CreatePendingActivation(PendingActivationState activationKey)
        {
            PendingActivation = activationKey;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates.SetState(PendingActivation.address, MarkChanged);
            }

            CheckPermission(context);

            return context.PreviousStates.SetState(
                PendingActivation.address,
                PendingActivation.Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = ((Bencodex.Types.Dictionary) plainValue);
            PendingActivation = new PendingActivationState((Dictionary) asDict["pending_activation"]);
        }
    }
}
