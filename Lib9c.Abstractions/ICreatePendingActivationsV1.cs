using System.Collections.Generic;
using Bencodex.Types;

namespace Lib9c.Abstractions
{
    public interface ICreatePendingActivationsV1
    {
        IEnumerable<IValue> PendingActivations { get; }
    }
}
