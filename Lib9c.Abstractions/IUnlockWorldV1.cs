using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IUnlockWorldV1
    {
        IEnumerable<int> WorldIds { get; }
        Address AvatarAddress { get; }
    }
}
