using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IUnlockWorldV1
    {
        IEnumerable<int> WorldIds { get; }
        Address AvatarAddress { get; }
    }
}
