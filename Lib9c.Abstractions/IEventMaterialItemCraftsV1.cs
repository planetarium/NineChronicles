using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IEventMaterialItemCraftsV1
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventMaterialItemRecipeId { get; }
        IReadOnlyDictionary<int, int> MaterialsToUse { get; }
    }
}
