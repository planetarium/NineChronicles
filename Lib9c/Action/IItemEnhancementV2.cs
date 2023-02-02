using System;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IItemEnhancementV2
    {
        Guid ItemId { get; }
        Guid MaterialId { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
