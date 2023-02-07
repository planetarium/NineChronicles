using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IRankingBattleV1
    {
        Address AvatarAddress { get; }
        Address EnemyAddress { get; }
        Address WeeklyArenaAddress { get; }
        IEnumerable<int> CostumeIds { get; }
        IEnumerable<Guid> EquipmentIds { get; }
        IEnumerable<Guid> ConsumableIds { get; }
    }
}
