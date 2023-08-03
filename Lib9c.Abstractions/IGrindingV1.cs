#nullable enable

using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IGrindingV1
    {
        Address AvatarAddress { get; }
        List<Guid> EquipmentsIds { get; }
        bool ChargeAp { get; }
    }
}
