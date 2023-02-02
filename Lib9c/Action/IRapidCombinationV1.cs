#nullable enable

using Libplanet;

namespace Nekoyume.Action
{
    public interface IRapidCombinationV1
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
