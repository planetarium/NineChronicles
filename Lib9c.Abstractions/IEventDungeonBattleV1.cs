using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IEventDungeonBattleV1
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventDungeonId { get; }
        int EventDungeonStageId { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Foods { get; }
        bool BuyTicketIfNeeded { get; }
    }
}
