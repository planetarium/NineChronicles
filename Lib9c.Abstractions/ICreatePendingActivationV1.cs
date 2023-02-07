using Bencodex.Types;

namespace Lib9c.Abstractions
{
    public interface ICreatePendingActivationV1
    {
        IValue PendingActivation { get; }
    }
}
