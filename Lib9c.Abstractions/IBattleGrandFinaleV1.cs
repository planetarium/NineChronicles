using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IBattleGrandFinaleV1
    {
        Address MyAvatarAddress { get; }
        Address EnemyAvatarAddress { get; }
        int GrandFinaleId { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
    }
}
