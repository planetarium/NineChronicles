using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IItemEnhancementV3
    {
        Guid ItemId { get; }
        Guid MaterialId { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int? PetId { get; }
    }
}
