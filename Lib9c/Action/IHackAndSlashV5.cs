using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IHackAndSlashV5
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        int WorldId { get; }
        int StageId { get; }
        int PlayCount { get; }
        Address AvatarAddress { get; }
    }
}
