using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Updated at https://github.com/planetarium/lib9c/pull/746
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("create_pending_activations")]
    [ActionObsolete(ActionObsoleteConfig.V200030ObsoleteIndex)]
    public class CreatePendingActivations : ActionBase, ICreatePendingActivationsV1
    {
        public IList<(byte[] Address, byte[] Nonce, byte[] PublicKey)> PendingActivations { get; internal set; }

        IEnumerable<IValue> ICreatePendingActivationsV1.PendingActivations =>
            PendingActivations.Select(t =>
                new List(new Binary[] { t.Address, t.Nonce, t.PublicKey }.Cast<IValue>()));

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "create_pending_activations")
            .Add("values", PendingActivations
                .Select(t => new List(new Binary[] { t.Address, t.Nonce, t.PublicKey }.Cast<IValue>()))
                .Serialize());

        public CreatePendingActivations()
        {
        }

        public CreatePendingActivations(IEnumerable<PendingActivationState> states)
        {
            PendingActivations = states.Select(pa => (pa.address.ToByteArray(), pa.Nonce, pa.PublicKey.Format(true))).ToImmutableList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            CheckObsolete(ActionObsoleteConfig.V200030ObsoleteIndex, context);
            CheckPermission(context);
            var state = context.PreviousState;
            foreach ((byte[] address, byte[] nonce, byte[] publicKey) in PendingActivations)
            {
                state = state.SetState(
                    new Address(address),
                    new Dictionary(
                        new[]
                        {
                            new KeyValuePair<IKey, IValue>((Text)"address", (Binary)address),
                            new KeyValuePair<IKey, IValue>((Text)"nonce", (Binary)nonce),
                            new KeyValuePair<IKey, IValue>((Text)"public_key", (Binary)publicKey),
                        }
                    )
                );
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            PendingActivations = ((List)((Dictionary)plainValue)["values"])
                .Cast<List>()
                .Select(v => (((Binary)v[0]).ToByteArray(), ((Binary)v[1]).ToByteArray(), ((Binary)v[2]).ToByteArray()))
                .ToImmutableList();
        }
    }
}
