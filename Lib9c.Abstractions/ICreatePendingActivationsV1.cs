using System.Collections.Generic;

namespace Lib9c.Abstractions
{
    public interface ICreatePendingActivationsV1
    {
        IEnumerable<Bencodex.Types.List> PendingActivations { get; }
    }
}
