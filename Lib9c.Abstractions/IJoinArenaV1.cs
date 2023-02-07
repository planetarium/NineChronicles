#nullable enable

using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IJoinArenaV1
    {
        Address AvatarAddress { get; }
        int ChampionshipId { get; }
        int Round { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<IValue>? RuneSlotInfos => null;
    }
}
