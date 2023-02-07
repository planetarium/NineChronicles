#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;
using BencodexList = Bencodex.Types.List;

namespace Lib9c.Abstractions
{
    public interface IJoinArenaV1
    {
        Address AvatarAddress { get; }
        int ChampionshipId { get; }
        int Round { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<BencodexList>? RuneSlotInfos => null;
    }
}
