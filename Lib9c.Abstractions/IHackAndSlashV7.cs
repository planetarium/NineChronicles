using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashV7
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        int WorldId { get; }
        int StageId { get; }
        int? StageBuffId { get; }
        Address AvatarAddress { get; }
    }
}
