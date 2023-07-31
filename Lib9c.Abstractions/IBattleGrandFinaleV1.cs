using System;
using System.Collections.Generic;
using Libplanet.Crypto;

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
