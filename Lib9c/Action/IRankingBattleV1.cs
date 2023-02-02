using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
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
