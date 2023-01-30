#nullable enable

using Libplanet;

namespace Nekoyume.Action
{
    public interface IRapidCombination
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
