#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Action
{
    public interface IRankingBattleV2
    {
        Address AvatarAddress { get; }
        Address EnemyAddress { get; }
        Address WeeklyArenaAddress { get; }
        IEnumerable<Guid> CostumeIds { get; }
        IEnumerable<Guid> EquipmentIds { get; }
        IEnumerable<Guid>? ConsumableIds => null;
    }
}
