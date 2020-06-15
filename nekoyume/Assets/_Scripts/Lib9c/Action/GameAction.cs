using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class GameAction : ActionBase
    {
        public Guid Id { get; private set; }
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(
                PlainValueInternal
                    .SetItem("id", Id.Serialize())
                    .Select(kv => new KeyValuePair<IKey, IValue>((Text) kv.Key, kv.Value))
            );
        protected abstract IImmutableDictionary<string, IValue> PlainValueInternal { get; }

        protected GameAction()
        {
            Id = Guid.NewGuid();
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = ((Bencodex.Types.Dictionary) plainValue)
                .Select(kv => new KeyValuePair<string, IValue>((Text) kv.Key, kv.Value))
                .ToImmutableDictionary();
            Id = dict["id"].ToGuid();
            LoadPlainValueInternal(dict);
        }
        
        protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue);

        protected void CheckPermission(IActionContext ctx)
        {
            IAccountStateDelta prevState = ctx.PreviousStates;
            IValue rawState = prevState.GetState(AdminState.Address);

            if (rawState is Bencodex.Types.Dictionary asDict)
            {
                var policy = new AdminState(asDict);

                if (ctx.BlockIndex > policy.ValidUntil)
                {
                    throw new PolicyExpiredException(policy, ctx.BlockIndex);
                }

                if (policy.AdminAddress != ctx.Signer)
                {
                    throw new PermissionDeniedException(policy, ctx.Signer);
                }
            }
        }
    }
}
