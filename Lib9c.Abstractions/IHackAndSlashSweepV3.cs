using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashSweepV3
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<IValue> RuneSlotInfos { get; }
        Address AvatarAddress { get; }
        int ApStoneCount { get; }
        int ActionPoint { get; }
        int WorldId { get; }
        int StageId { get; }
    }
}
