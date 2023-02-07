using System.Collections.Generic;
using Bencodex.Types;

namespace Lib9c.Abstractions
{
    public interface IMigrationAvatarStateV1
    {
        IEnumerable<IValue> AvatarStates { get; }
    }
}
