using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IItemEnhancementV2
    {
        Guid ItemId { get; }
        Guid MaterialId { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
