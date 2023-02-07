#nullable enable

using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IRapidCombinationV1
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
