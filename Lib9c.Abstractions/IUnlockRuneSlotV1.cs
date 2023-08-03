using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IUnlockRuneSlotV1
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
