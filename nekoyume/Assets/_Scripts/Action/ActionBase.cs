using System.Collections.Immutable;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public abstract void LoadPlainValue(IImmutableDictionary<string, object> plainValue);

        public abstract IAccountStateDelta Execute(IActionContext ctx);

        public abstract IImmutableDictionary<string, object> PlainValue { get; }
    }
}
