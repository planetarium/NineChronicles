using System.Collections.Generic;
using Bencodex.Types;

namespace Nekoyume.Action
{
    public interface IMigrationAvatarStateV1
    {
        IEnumerable<IValue> AvatarStates { get; }
    }
}
