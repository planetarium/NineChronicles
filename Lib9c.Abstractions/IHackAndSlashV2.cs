using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashV2
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        int WorldId { get; }
        int StageId { get; }
        Address AvatarAddress { get; }
        Address WeeklyArenaAddress { get; }
        Address RankingMapAddress { get; }
    }
}
