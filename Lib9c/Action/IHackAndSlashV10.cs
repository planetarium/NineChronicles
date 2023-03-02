using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IHackAndSlashV10
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        IEnumerable<IValue> RuneSlotInfos { get; }
        int WorldId { get; }
        int StageId { get; }
        int? StageBuffId { get; }
        int TotalPlayCount { get; }
        int ApStoneCount { get; }
        Address AvatarAddress { get; }
    }
}
