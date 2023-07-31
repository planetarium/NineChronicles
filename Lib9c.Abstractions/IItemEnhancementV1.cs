using System;
using System.Collections.Generic;
using Libplanet.Crypto;

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
