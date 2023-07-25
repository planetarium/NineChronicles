using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashSweepV2
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        Address AvatarAddress { get; }
        int ApStoneCount { get; }
        int ActionPoint { get; }
        int WorldId { get; }
        int StageId { get; }
    }
}
