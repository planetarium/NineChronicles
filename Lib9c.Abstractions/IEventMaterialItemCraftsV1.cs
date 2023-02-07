using System.Collections.Generic;
using Libplanet;

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
