using System;
using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IRaidV1
    {
        Address AvatarAddress { get; }
        IEnumerable<Guid> EquipmentIds { get; }
        IEnumerable<Guid> CostumeIds { get; }
        IEnumerable<Guid> FoodIds { get; }
        bool PayNcg { get; }
    }
}
