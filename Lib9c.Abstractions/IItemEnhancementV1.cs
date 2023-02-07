using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IItemEnhancementV1
    {
        Guid ItemId { get; }
        IEnumerable<Guid> MaterialIds { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
