using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IEventMaterialItemCraftsV1
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventMaterialItemRecipeId { get; }
        IReadOnlyDictionary<int, int> MaterialsToUse { get; }
    }
}
