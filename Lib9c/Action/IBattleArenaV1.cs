#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;

using BencodexList = Bencodex.Types.List;

namespace Nekoyume.Action
{
    public interface IBattleArenaV1
    {
        Address MyAvatarAddress { get; }
        Address EnemyAvatarAddress { get; }
        int ChampionshipId { get; }
        int Round { get; }
        int Ticket { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<BencodexList>? RuneSlotInfos => null;
    }
}
