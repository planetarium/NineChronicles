
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("create_activation_key")]
    public class CreateActivationKey : ActionBase
    {
        public ActivationKeyState ActivationKey { get; private set; }

        public override IValue PlainValue
            => new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"activation_key",
                        ActivationKey.Serialize()
                    ),
                }
            );

        public CreateActivationKey()
        {
        }

        public CreateActivationKey(ActivationKeyState activationKey)
        {
            ActivationKey = activationKey;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            CheckPermission(context);

            return context.PreviousStates.SetState(
                ActivationKey.address,
                ActivationKey.Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = ((Bencodex.Types.Dictionary) plainValue);
            ActivationKey = new ActivationKeyState((Dictionary) asDict["activation_key"]);
        }
    }
}
