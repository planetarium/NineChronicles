#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;
using BencodexList = Bencodex.Types.List;

namespace Nekoyume.Action
{
    public interface IJoinArena
    {
        Address AvatarAddress { get; }
        int ChampionshipId { get; }
        int Round { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<BencodexList>? RuneSlotInfos => null;
    }
}
