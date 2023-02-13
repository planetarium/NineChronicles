#nullable enable

using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Lib9c.Abstractions
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
        IEnumerable<IValue>? RuneSlotInfos => null;
    }
}
