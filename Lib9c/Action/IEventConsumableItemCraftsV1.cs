using Libplanet;

namespace Nekoyume.Action
{
    public interface IEventConsumableItemCraftsV1
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventConsumableItemRecipeId { get; }
        int SlotIndex { get; }
    }
}
