using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.State;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("create_pending_activation")]
    [ActionObsolete(MeadConfig.MeadTransferStartIndex)]
    public class CreatePendingActivation : ActionBase, ICreatePendingActivationV1
    {
        public PendingActivationState PendingActivation { get; private set; }

        IValue ICreatePendingActivationV1.PendingActivation => PendingActivation.Serialize();

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "create_pending_activation")
            .Add("values", new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"pending_activation",
                        PendingActivation.Serialize()
                    ),
                }
            ));

        public CreatePendingActivation()
        {
        }

        public CreatePendingActivation(PendingActivationState activationKey)
        {
            PendingActivation = activationKey;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousStates.SetState(PendingActivation.address, MarkChanged);
            }

            CheckObsolete(MeadConfig.MeadTransferStartIndex, context);
            CheckPermission(context);

            return context.PreviousStates.SetState(
                PendingActivation.address,
                PendingActivation.Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            PendingActivation = new PendingActivationState((Dictionary) asDict["pending_activation"]);
        }
    }
}
