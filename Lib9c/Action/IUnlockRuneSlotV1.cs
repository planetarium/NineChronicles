using Libplanet;

namespace Nekoyume.Action
{
    public interface IUnlockRuneSlotV1
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
