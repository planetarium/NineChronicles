using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IHackAndSlashV1
    {
        IEnumerable<int> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        int WorldId { get; }
        int StageId { get; }
        Address AvatarAddress { get; }
        Address WeeklyArenaAddress { get; }
        Address RankingMapAddress { get; }
    }
}
