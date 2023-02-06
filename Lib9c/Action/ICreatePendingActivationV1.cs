using Bencodex.Types;

namespace Nekoyume.Action
{
    public interface ICreatePendingActivationV1
    {
        IValue PendingActivation { get; }
    }
}
