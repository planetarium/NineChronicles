using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IItemEnhancementV4
    {
        Guid ItemId { get; }
        List<Guid> MaterialIds { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
