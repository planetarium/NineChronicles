using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IMimisbrunnrBattleV5
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        IEnumerable<IValue> RuneSlotInfos { get; }
        int WorldId { get; }
        int StageId { get; }
        int PlayCount { get; }
        Address AvatarAddress { get; }
    }
}
