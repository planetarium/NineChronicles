using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IUnlockRuneSlotV1
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
