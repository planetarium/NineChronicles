using System.Collections.Generic;

namespace Nekoyume.Action
{
    public interface ICreatePendingActivationsV1
    {
        IEnumerable<Bencodex.Types.List> PendingActivations { get; }
    }
}
