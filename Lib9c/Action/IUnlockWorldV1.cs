using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IUnlockWorldV1
    {
        IEnumerable<int> WorldIds { get; }
        Address AvatarAddress { get; }
    }
}
