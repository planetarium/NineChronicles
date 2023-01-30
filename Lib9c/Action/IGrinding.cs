#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IGrinding
    {
        Address AvatarAddress { get; }
        List<Guid> EquipmentsIds { get; }
        bool ChargeAp { get; }
    }
}
