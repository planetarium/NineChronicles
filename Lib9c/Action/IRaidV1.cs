using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
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
